window.epMobilePlay = function(p) { if (!p || typeof p.playVideo !== 'function') return; try { p.playVideo(); setTimeout(function() { if (typeof p.getPlayerState === 'function' && p.getPlayerState() !== 1 && typeof p.mute === 'function') { p.mute(); p.playVideo(); setTimeout(function() { if (typeof p.unMute === 'function') p.unMute(); }, 300); } }, 250); } catch (e) {} };
window.EveryPageStorage = {
    get: function (key) {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    set: function (key, value) {
        try {
            if (value === null || value === undefined || value === "") {
                localStorage.removeItem(key);
            } else {
                localStorage.setItem(key, value);
            }
        } catch { }
    }
};

window.EveryPageTheme = {
    set: function (themeId) {
        let t = themeId || "charlie-brown";
        if (t === "bw-minimal") t = "snoopy-day";
        if (t === "peppermint-patty") t = "charlie-brown";
        document.documentElement.setAttribute("data-theme", t);
        document.documentElement.removeAttribute("data-mode");
        window.EveryPageStorage.set("everypage_peanuts_theme", t);
    }
};

window.EveryPageMusic = (function () {
    let dotNetRef = null;
    let ytPlayer = null;
    let ytReady = false;
    let pendingPlay = null;
    let progressTimer = null;
    let currentVolume = 10;

    function loadYouTubeApi() {
        if (window.YT && window.YT.Player) {
            createPlayer();
            return;
        }
        if (!document.getElementById("ep-yt-iframe-api")) {
            const tag = document.createElement("script");
            tag.id = "ep-yt-iframe-api";
            tag.src = "https://www.youtube.com/iframe_api";
            document.head.appendChild(tag);
        }
        const prevReady = window.onYouTubeIframeAPIReady;
        window.onYouTubeIframeAPIReady = function () {
            if (typeof prevReady === "function") prevReady();
            createPlayer();
        };
    }

    function createPlayer() {
        const host = document.getElementById("ep-matchbox-yt-host");
        if (!host || ytPlayer) return;

        ytPlayer = new YT.Player("ep-matchbox-yt-host", {
            height: "120",
            width: "120",
            playerVars: {
                autoplay: 1,
                controls: 0,
                disablekb: 1,
                fs: 0,
                iv_load_policy: 3,
                modestbranding: 1,
                playsinline: 1,
                rel: 0
            },
            events: {
                onReady: function () {
                    ytReady = true;
                    ytPlayer.setVolume(currentVolume);
                    if (pendingPlay) {
                        const p = pendingPlay;
                        pendingPlay = null;
                        playDirect(p.videoId, p.playlistId, p.volume);
                    }
                },
                onStateChange: function (event) {
                    if (event.data === YT.PlayerState.PLAYING) {
                        ytPlayer.setVolume(currentVolume);
                    } else if (event.data === YT.PlayerState.ENDED && dotNetRef) {
                        dotNetRef.invokeMethodAsync("OnTrackEnded");
                    }
                },
                onError: function () {
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync("OnTrackEnded");
                    }
                }
            }
        });

        if (progressTimer) clearInterval(progressTimer);
        progressTimer = setInterval(function () {
            if (!ytReady || !ytPlayer || typeof ytPlayer.getCurrentTime !== "function" || !dotNetRef) return;
            const state = ytPlayer.getPlayerState();
            const playing = state === YT.PlayerState.PLAYING || state === YT.PlayerState.BUFFERING;
            const cur = Math.floor(ytPlayer.getCurrentTime() || 0);
            const dur = Math.floor(ytPlayer.getDuration() || 0);
            let liveTitle = null;
            let liveArtist = null;
            if (typeof ytPlayer.getVideoData === "function") {
                const vd = ytPlayer.getVideoData();
                if (vd && vd.title) liveTitle = vd.title;
                if (vd && vd.author) liveArtist = vd.author;
            }
            dotNetRef.invokeMethodAsync("OnPlayerProgress", cur, dur, playing, liveTitle, liveArtist);
        }, 800);
    }

    function isYouTubeDomain(input) {
        return /\b(youtube\.com|youtu\.be|music\.youtube\.com)\b/i.test(input);
    }

    function extractYouTubeIds(input) {
        const videoMatch = input.match(/(?:(?:music\.)?youtube\.com\/(?:watch\?(?:.*&)?v=|embed\/|shorts\/|live\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})/i);
        const listMatch = input.match(/[?&]list=([a-zA-Z0-9_-]+)/i);
        return {
            videoId: videoMatch ? videoMatch[1] : null,
            playlistId: listMatch ? listMatch[1] : null
        };
    }

    async function fetchWithTimeout(url, ms) {
        const ctrl = new AbortController();
        const timer = setTimeout(() => ctrl.abort(), ms || 3500);
        try {
            const res = await fetch(url, { signal: ctrl.signal });
            if (!res.ok) throw new Error("HTTP " + res.status);
            return await res.json();
        } finally {
            clearTimeout(timer);
        }
    }

    async function searchOrResolve(input) {
        const trimmed = (input || "").trim();
        if (!trimmed) {
            return {
                status: "invalid",
                errorMessage: "Please insert a valid YouTube link (youtube.com or youtu.be).",
                sourceLabel: null,
                items: null
            };
        }

        if (!isYouTubeDomain(trimmed)) {
            return {
                status: "invalid",
                errorMessage: "Only YouTube links (youtube.com, youtu.be, music.youtube.com) are accepted.",
                sourceLabel: null,
                items: null
            };
        }

        const { videoId, playlistId } = extractYouTubeIds(trimmed);
        if (!videoId && !playlistId) {
            return {
                status: "invalid",
                errorMessage: "That YouTube link doesn't point to a valid video or playlist. Paste a youtube.com/watch?v=..., youtu.be/..., or playlist?list=... URL.",
                sourceLabel: null,
                items: null
            };
        }

        if (videoId) {
            let title = "YouTube Video";
            let artist = "YouTube";
            try {
                const meta = await fetchWithTimeout(
                    "https://www.youtube.com/oembed?url=" + encodeURIComponent("https://www.youtube.com/watch?v=" + videoId) + "&format=json",
                    3500
                );
                if (meta?.title) title = meta.title;
                if (meta?.author_name) artist = meta.author_name;
            } catch { }

            return {
                status: "ok",
                errorMessage: null,
                sourceLabel: "YouTube Link",
                items: [{ title: title, artist: artist, videoId: videoId, playlistId: playlistId, duration: "" }]
            };
        }

        return {
            status: "ok",
            errorMessage: null,
            sourceLabel: "YouTube Playlist",
            items: [{ title: "YouTube Playlist", artist: "YouTube", videoId: null, playlistId: playlistId, duration: "" }]
        };
    }

    function playDirect(videoId, playlistId, volume) {
        if (typeof volume === "number") {
            currentVolume = volume;
        }

        if (!ytReady || !ytPlayer) {
            pendingPlay = { videoId, playlistId, volume: currentVolume };
            return;
        }

        try { ytPlayer.stopVideo(); } catch { }
        ytPlayer.setVolume(currentVolume);

        if (playlistId && !videoId) {
            ytPlayer.loadPlaylist({ listType: "playlist", list: playlistId, index: 0, startSeconds: 0 });
            ytPlayer.playVideo();
            return;
        }

        if (videoId) {
            ytPlayer.loadVideoById({ videoId: videoId, startSeconds: 0 });
            ytPlayer.playVideo();
        }
    }

    function togglePlay() {
        if (!ytReady || !ytPlayer) return false;
        const state = ytPlayer.getPlayerState();
        if (state === YT.PlayerState.PLAYING || state === YT.PlayerState.BUFFERING) {
            ytPlayer.pauseVideo();
            return false;
        }
        ytPlayer.setVolume(currentVolume);
        ytPlayer.playVideo();
        return true;
    }

    function nextInPlaylist() {
        if (ytReady && ytPlayer && typeof ytPlayer.nextVideo === "function") {
            ytPlayer.nextVideo();
        }
    }

    function prevInPlaylist() {
        if (ytReady && ytPlayer && typeof ytPlayer.previousVideo === "function") {
            ytPlayer.previousVideo();
        }
    }

    function seekTo(sec) {
        if (ytReady && ytPlayer && typeof ytPlayer.seekTo === "function") {
            ytPlayer.seekTo(sec, true);
        }
    }

    function setVolume(vol) {
        currentVolume = Math.max(0, Math.min(100, Number(vol) || 0));
        if (ytReady && ytPlayer && typeof ytPlayer.setVolume === "function") {
            ytPlayer.setVolume(currentVolume);
        }
    }

    return {
        init: function (ref, initialVol) {
            dotNetRef = ref;
            if (typeof initialVol === "number") currentVolume = initialVol;
            loadYouTubeApi();
        },
        searchOrResolve: searchOrResolve,
        playDirect: playDirect,
        togglePlay: togglePlay,
        nextInPlaylist: nextInPlaylist,
        prevInPlaylist: prevInPlaylist,
        seekTo: seekTo,
        setVolume: setVolume,
        dispose: function () {
            if (progressTimer) clearInterval(progressTimer);
            dotNetRef = null;
        }
    };
})();