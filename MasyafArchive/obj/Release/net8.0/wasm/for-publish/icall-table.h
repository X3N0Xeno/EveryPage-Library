#define ICALL_TABLE_corlib 1

static int corlib_icall_indexes [] = {
224,
234,
235,
236,
237,
238,
239,
240,
241,
242,
245,
246,
329,
330,
331,
360,
361,
362,
382,
383,
384,
385,
483,
484,
485,
488,
528,
529,
531,
533,
535,
537,
542,
550,
551,
552,
553,
554,
555,
556,
557,
558,
559,
655,
656,
721,
727,
730,
732,
737,
738,
740,
741,
745,
746,
748,
750,
751,
754,
755,
756,
759,
761,
764,
766,
768,
777,
841,
843,
845,
855,
856,
857,
859,
865,
866,
867,
868,
869,
877,
878,
879,
883,
884,
886,
888,
1094,
1269,
1270,
7166,
7167,
7169,
7170,
7171,
7172,
7173,
7175,
7177,
7179,
7180,
7190,
7192,
7197,
7199,
7201,
7203,
7252,
7258,
7259,
7261,
7262,
7263,
7264,
7265,
7267,
7269,
8323,
8327,
8329,
8330,
8331,
8332,
8581,
8582,
8583,
8584,
8602,
8603,
8604,
8606,
8654,
8732,
8734,
8736,
8745,
8746,
8747,
8748,
9192,
9196,
9197,
9224,
9242,
9249,
9256,
9267,
9271,
9291,
9364,
9366,
9375,
9377,
9378,
9385,
9399,
9419,
9420,
9428,
9430,
9437,
9438,
9441,
9443,
9448,
9454,
9455,
9462,
9464,
9476,
9479,
9480,
9481,
9492,
9501,
9507,
9508,
9509,
9511,
9512,
9529,
9531,
9545,
9562,
9589,
9619,
9620,
10103,
10195,
10196,
10396,
10397,
10404,
10405,
10406,
10411,
10486,
10952,
10953,
11322,
11323,
11324,
11329,
11339,
12175,
12196,
12198,
12200,
};
void ves_icall_System_Array_InternalCreate (int,int,int,int,int);
int ves_icall_System_Array_GetCorElementTypeOfElementTypeInternal (int);
int ves_icall_System_Array_IsValueOfElementTypeInternal (int,int);
int ves_icall_System_Array_CanChangePrimitive (int,int,int);
int ves_icall_System_Array_FastCopy (int,int,int,int,int);
int ves_icall_System_Array_GetLengthInternal_raw (int,int,int);
int ves_icall_System_Array_GetLowerBoundInternal_raw (int,int,int);
void ves_icall_System_Array_GetGenericValue_icall (int,int,int);
void ves_icall_System_Array_GetValueImpl_raw (int,int,int,int);
void ves_icall_System_Array_SetGenericValue_icall (int,int,int);
void ves_icall_System_Array_SetValueImpl_raw (int,int,int,int);
void ves_icall_System_Array_SetValueRelaxedImpl_raw (int,int,int,int);
void ves_icall_System_Runtime_RuntimeImports_ZeroMemory (int,int);
void ves_icall_System_Runtime_RuntimeImports_Memmove (int,int,int);
void ves_icall_System_Buffer_BulkMoveWithWriteBarrier (int,int,int,int);
int ves_icall_System_Delegate_AllocDelegateLike_internal_raw (int,int);
int ves_icall_System_Delegate_CreateDelegate_internal_raw (int,int,int,int,int);
int ves_icall_System_Delegate_GetVirtualMethod_internal_raw (int,int);
void ves_icall_System_Enum_GetEnumValuesAndNames_raw (int,int,int,int);
void ves_icall_System_Enum_InternalBoxEnum_raw (int,int,int64_t,int);
int ves_icall_System_Enum_InternalGetCorElementType (int);
void ves_icall_System_Enum_InternalGetUnderlyingType_raw (int,int,int);
int ves_icall_System_Environment_get_ProcessorCount ();
int ves_icall_System_Environment_get_TickCount ();
int64_t ves_icall_System_Environment_get_TickCount64 ();
void ves_icall_System_Environment_FailFast_raw (int,int,int,int);
void ves_icall_System_GC_register_ephemeron_array_raw (int,int);
int ves_icall_System_GC_get_ephemeron_tombstone_raw (int);
void ves_icall_System_GC_SuppressFinalize_raw (int,int);
void ves_icall_System_GC_ReRegisterForFinalize_raw (int,int);
void ves_icall_System_GC_GetGCMemoryInfo (int,int,int,int,int,int);
int ves_icall_System_GC_AllocPinnedArray_raw (int,int,int);
int ves_icall_System_Object_MemberwiseClone_raw (int,int);
double ves_icall_System_Math_Ceiling (double);
double ves_icall_System_Math_Cos (double);
double ves_icall_System_Math_Floor (double);
double ves_icall_System_Math_Log (double);
double ves_icall_System_Math_Log10 (double);
double ves_icall_System_Math_Pow (double,double);
double ves_icall_System_Math_Sin (double);
double ves_icall_System_Math_Sqrt (double);
double ves_icall_System_Math_Tan (double);
double ves_icall_System_Math_ModF (double,int);
void ves_icall_RuntimeMethodHandle_ReboxFromNullable_raw (int,int,int);
void ves_icall_RuntimeMethodHandle_ReboxToNullable_raw (int,int,int,int);
int ves_icall_RuntimeType_GetCorrespondingInflatedMethod_raw (int,int,int);
void ves_icall_RuntimeType_make_array_type_raw (int,int,int,int);
void ves_icall_RuntimeType_make_byref_type_raw (int,int,int);
void ves_icall_RuntimeType_make_pointer_type_raw (int,int,int);
void ves_icall_RuntimeType_MakeGenericType_raw (int,int,int,int);
int ves_icall_RuntimeType_GetMethodsByName_native_raw (int,int,int,int,int);
int ves_icall_RuntimeType_GetPropertiesByName_native_raw (int,int,int,int,int);
int ves_icall_RuntimeType_GetConstructors_native_raw (int,int,int);
int ves_icall_System_RuntimeType_CreateInstanceInternal_raw (int,int);
void ves_icall_System_RuntimeType_AllocateValueType_raw (int,int,int,int);
void ves_icall_RuntimeType_GetDeclaringMethod_raw (int,int,int);
void ves_icall_System_RuntimeType_getFullName_raw (int,int,int,int,int);
void ves_icall_RuntimeType_GetGenericArgumentsInternal_raw (int,int,int,int);
int ves_icall_RuntimeType_GetGenericParameterPosition (int);
int ves_icall_RuntimeType_GetEvents_native_raw (int,int,int,int);
int ves_icall_RuntimeType_GetFields_native_raw (int,int,int,int,int);
void ves_icall_RuntimeType_GetInterfaces_raw (int,int,int);
int ves_icall_RuntimeType_GetNestedTypes_native_raw (int,int,int,int,int);
void ves_icall_RuntimeType_GetDeclaringType_raw (int,int,int);
void ves_icall_RuntimeType_GetName_raw (int,int,int);
void ves_icall_RuntimeType_GetNamespace_raw (int,int,int);
int ves_icall_RuntimeType_FunctionPointerReturnAndParameterTypes_raw (int,int);
int ves_icall_RuntimeTypeHandle_GetAttributes (int);
int ves_icall_RuntimeTypeHandle_GetMetadataToken_raw (int,int);
void ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl_raw (int,int,int);
int ves_icall_RuntimeTypeHandle_GetCorElementType (int);
int ves_icall_RuntimeTypeHandle_HasInstantiation (int);
int ves_icall_RuntimeTypeHandle_IsInstanceOfType_raw (int,int,int);
int ves_icall_RuntimeTypeHandle_HasReferences_raw (int,int);
int ves_icall_RuntimeTypeHandle_GetArrayRank_raw (int,int);
void ves_icall_RuntimeTypeHandle_GetAssembly_raw (int,int,int);
void ves_icall_RuntimeTypeHandle_GetElementType_raw (int,int,int);
void ves_icall_RuntimeTypeHandle_GetModule_raw (int,int,int);
void ves_icall_RuntimeTypeHandle_GetBaseType_raw (int,int,int);
int ves_icall_RuntimeTypeHandle_type_is_assignable_from_raw (int,int,int);
int ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition (int);
int ves_icall_RuntimeTypeHandle_GetGenericParameterInfo_raw (int,int);
int ves_icall_RuntimeTypeHandle_is_subclass_of_raw (int,int,int);
int ves_icall_RuntimeTypeHandle_IsByRefLike_raw (int,int);
void ves_icall_System_RuntimeTypeHandle_internal_from_name_raw (int,int,int,int,int,int);
int ves_icall_System_String_FastAllocateString_raw (int,int);
int ves_icall_System_Type_internal_from_handle_raw (int,int);
int ves_icall_System_ValueType_InternalGetHashCode_raw (int,int,int);
int ves_icall_System_ValueType_Equals_raw (int,int,int,int);
int ves_icall_System_Threading_Interlocked_CompareExchange_Int (int,int,int);
void ves_icall_System_Threading_Interlocked_CompareExchange_Object (int,int,int,int);
int ves_icall_System_Threading_Interlocked_Decrement_Int (int);
int ves_icall_System_Threading_Interlocked_Increment_Int (int);
int64_t ves_icall_System_Threading_Interlocked_Increment_Long (int);
int ves_icall_System_Threading_Interlocked_Exchange_Int (int,int);
void ves_icall_System_Threading_Interlocked_Exchange_Object (int,int,int);
int64_t ves_icall_System_Threading_Interlocked_CompareExchange_Long (int,int64_t,int64_t);
int64_t ves_icall_System_Threading_Interlocked_Exchange_Long (int,int64_t);
int ves_icall_System_Threading_Interlocked_Add_Int (int,int);
int64_t ves_icall_System_Threading_Interlocked_Add_Long (int,int64_t);
void ves_icall_System_Threading_Monitor_Monitor_Enter_raw (int,int);
void mono_monitor_exit_icall_raw (int,int);
void ves_icall_System_Threading_Monitor_Monitor_pulse_raw (int,int);
void ves_icall_System_Threading_Monitor_Monitor_pulse_all_raw (int,int);
int ves_icall_System_Threading_Monitor_Monitor_wait_raw (int,int,int,int);
void ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var_raw (int,int,int,int,int);
void ves_icall_System_Threading_Thread_StartInternal_raw (int,int,int);
void ves_icall_System_Threading_Thread_InitInternal_raw (int,int);
int ves_icall_System_Threading_Thread_GetCurrentThread ();
void ves_icall_System_Threading_InternalThread_Thread_free_internal_raw (int,int);
int ves_icall_System_Threading_Thread_GetState_raw (int,int);
void ves_icall_System_Threading_Thread_SetState_raw (int,int,int);
void ves_icall_System_Threading_Thread_ClrState_raw (int,int,int);
void ves_icall_System_Threading_Thread_SetName_icall_raw (int,int,int,int);
int ves_icall_System_Threading_Thread_YieldInternal ();
void ves_icall_System_Threading_Thread_SetPriority_raw (int,int,int);
void ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease_raw (int,int,int);
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly_raw (int,int);
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile_raw (int,int,int,int);
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC_raw (int,int,int,int,int);
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream_raw (int,int,int,int,int,int);
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies_raw (int);
int ves_icall_System_GCHandle_InternalAlloc_raw (int,int,int);
void ves_icall_System_GCHandle_InternalFree_raw (int,int);
int ves_icall_System_GCHandle_InternalGet_raw (int,int);
void ves_icall_System_GCHandle_InternalSet_raw (int,int,int);
int ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError ();
void ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError (int);
void ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr_raw (int,int,int,int);
int ves_icall_System_Runtime_InteropServices_Marshal_SizeOfHelper_raw (int,int,int);
int ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName_raw (int,int,int,int,int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalGetHashCode_raw (int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalTryGetHashCode_raw (int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue_raw (int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal_raw (int,int);
void ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray_raw (int,int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetSpanDataFrom_raw (int,int,int,int);
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack ();
int ves_icall_System_Reflection_Assembly_GetEntryAssembly_raw (int);
int ves_icall_System_Reflection_Assembly_InternalLoad_raw (int,int,int,int);
int ves_icall_System_Reflection_Assembly_InternalGetType_raw (int,int,int,int,int,int);
int ves_icall_System_Reflection_AssemblyName_GetNativeName (int);
int ves_icall_MonoCustomAttrs_GetCustomAttributesInternal_raw (int,int,int,int);
int ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal_raw (int,int);
int ves_icall_MonoCustomAttrs_IsDefinedInternal_raw (int,int,int);
int ves_icall_System_Reflection_FieldInfo_internal_from_handle_type_raw (int,int,int);
int ves_icall_System_Reflection_FieldInfo_get_marshal_info_raw (int,int);
int ves_icall_System_Reflection_LoaderAllocatorScout_Destroy (int);
void ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames_raw (int,int,int);
void ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes_raw (int,int,int);
void ves_icall_System_Reflection_RuntimeAssembly_GetInfo_raw (int,int,int,int);
int ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal_raw (int,int,int,int,int);
void ves_icall_System_Reflection_Assembly_GetManifestModuleInternal_raw (int,int,int);
void ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal_raw (int,int,int,int,int,int,int);
void ves_icall_RuntimeEventInfo_get_event_info_raw (int,int,int);
int ves_icall_reflection_get_token_raw (int,int);
int ves_icall_System_Reflection_EventInfo_internal_from_handle_type_raw (int,int,int);
int ves_icall_RuntimeFieldInfo_ResolveType_raw (int,int);
int ves_icall_RuntimeFieldInfo_GetParentType_raw (int,int,int);
int ves_icall_RuntimeFieldInfo_GetFieldOffset_raw (int,int);
int ves_icall_RuntimeFieldInfo_GetValueInternal_raw (int,int,int);
void ves_icall_RuntimeFieldInfo_SetValueInternal_raw (int,int,int,int);
int ves_icall_RuntimeFieldInfo_GetRawConstantValue_raw (int,int);
int ves_icall_reflection_get_token_raw (int,int);
void ves_icall_get_method_info_raw (int,int,int);
int ves_icall_get_method_attributes (int);
int ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info_raw (int,int,int);
int ves_icall_System_MonoMethodInfo_get_retval_marshal_raw (int,int);
int ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native_raw (int,int,int,int);
int ves_icall_RuntimeMethodInfo_get_name_raw (int,int);
int ves_icall_RuntimeMethodInfo_get_base_method_raw (int,int,int);
int ves_icall_reflection_get_token_raw (int,int);
int ves_icall_InternalInvoke_raw (int,int,int,int,int);
void ves_icall_RuntimeMethodInfo_GetPInvoke_raw (int,int,int,int,int);
int ves_icall_RuntimeMethodInfo_MakeGenericMethod_impl_raw (int,int,int);
int ves_icall_RuntimeMethodInfo_GetGenericArguments_raw (int,int);
int ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition_raw (int,int);
int ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition_raw (int,int);
int ves_icall_RuntimeMethodInfo_get_IsGenericMethod_raw (int,int);
void ves_icall_InvokeClassConstructor_raw (int,int);
int ves_icall_InternalInvoke_raw (int,int,int,int,int);
int ves_icall_reflection_get_token_raw (int,int);
int ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken_raw (int,int,int,int,int,int);
void ves_icall_RuntimePropertyInfo_get_property_info_raw (int,int,int,int);
int ves_icall_reflection_get_token_raw (int,int);
int ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type_raw (int,int,int);
void ves_icall_DynamicMethod_create_dynamic_method_raw (int,int,int,int,int);
void ves_icall_AssemblyBuilder_basic_init_raw (int,int);
void ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes_raw (int,int);
void ves_icall_ModuleBuilder_basic_init_raw (int,int);
void ves_icall_ModuleBuilder_set_wrappers_type_raw (int,int,int);
int ves_icall_ModuleBuilder_getUSIndex_raw (int,int,int);
int ves_icall_ModuleBuilder_getToken_raw (int,int,int,int);
int ves_icall_ModuleBuilder_getMethodToken_raw (int,int,int,int);
void ves_icall_ModuleBuilder_RegisterToken_raw (int,int,int,int);
int ves_icall_TypeBuilder_create_runtime_class_raw (int,int);
int ves_icall_System_IO_Stream_HasOverriddenBeginEndRead_raw (int,int);
int ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite_raw (int,int);
int ves_icall_System_Diagnostics_Debugger_IsAttached_internal ();
int ves_icall_System_Diagnostics_Debugger_IsLogging ();
void ves_icall_System_Diagnostics_Debugger_Log (int,int,int);
int ves_icall_System_Diagnostics_StackFrame_GetFrameInfo (int,int,int,int,int,int,int,int);
void ves_icall_System_Diagnostics_StackTrace_GetTrace (int,int,int,int);
int ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (int);
void ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (int);
int ves_icall_Mono_SafeStringMarshal_StringToUtf8 (int);
void ves_icall_Mono_SafeStringMarshal_GFree (int);
static void *corlib_icall_funcs [] = {
// token 224,
ves_icall_System_Array_InternalCreate,
// token 234,
ves_icall_System_Array_GetCorElementTypeOfElementTypeInternal,
// token 235,
ves_icall_System_Array_IsValueOfElementTypeInternal,
// token 236,
ves_icall_System_Array_CanChangePrimitive,
// token 237,
ves_icall_System_Array_FastCopy,
// token 238,
ves_icall_System_Array_GetLengthInternal_raw,
// token 239,
ves_icall_System_Array_GetLowerBoundInternal_raw,
// token 240,
ves_icall_System_Array_GetGenericValue_icall,
// token 241,
ves_icall_System_Array_GetValueImpl_raw,
// token 242,
ves_icall_System_Array_SetGenericValue_icall,
// token 245,
ves_icall_System_Array_SetValueImpl_raw,
// token 246,
ves_icall_System_Array_SetValueRelaxedImpl_raw,
// token 329,
ves_icall_System_Runtime_RuntimeImports_ZeroMemory,
// token 330,
ves_icall_System_Runtime_RuntimeImports_Memmove,
// token 331,
ves_icall_System_Buffer_BulkMoveWithWriteBarrier,
// token 360,
ves_icall_System_Delegate_AllocDelegateLike_internal_raw,
// token 361,
ves_icall_System_Delegate_CreateDelegate_internal_raw,
// token 362,
ves_icall_System_Delegate_GetVirtualMethod_internal_raw,
// token 382,
ves_icall_System_Enum_GetEnumValuesAndNames_raw,
// token 383,
ves_icall_System_Enum_InternalBoxEnum_raw,
// token 384,
ves_icall_System_Enum_InternalGetCorElementType,
// token 385,
ves_icall_System_Enum_InternalGetUnderlyingType_raw,
// token 483,
ves_icall_System_Environment_get_ProcessorCount,
// token 484,
ves_icall_System_Environment_get_TickCount,
// token 485,
ves_icall_System_Environment_get_TickCount64,
// token 488,
ves_icall_System_Environment_FailFast_raw,
// token 528,
ves_icall_System_GC_register_ephemeron_array_raw,
// token 529,
ves_icall_System_GC_get_ephemeron_tombstone_raw,
// token 531,
ves_icall_System_GC_SuppressFinalize_raw,
// token 533,
ves_icall_System_GC_ReRegisterForFinalize_raw,
// token 535,
ves_icall_System_GC_GetGCMemoryInfo,
// token 537,
ves_icall_System_GC_AllocPinnedArray_raw,
// token 542,
ves_icall_System_Object_MemberwiseClone_raw,
// token 550,
ves_icall_System_Math_Ceiling,
// token 551,
ves_icall_System_Math_Cos,
// token 552,
ves_icall_System_Math_Floor,
// token 553,
ves_icall_System_Math_Log,
// token 554,
ves_icall_System_Math_Log10,
// token 555,
ves_icall_System_Math_Pow,
// token 556,
ves_icall_System_Math_Sin,
// token 557,
ves_icall_System_Math_Sqrt,
// token 558,
ves_icall_System_Math_Tan,
// token 559,
ves_icall_System_Math_ModF,
// token 655,
ves_icall_RuntimeMethodHandle_ReboxFromNullable_raw,
// token 656,
ves_icall_RuntimeMethodHandle_ReboxToNullable_raw,
// token 721,
ves_icall_RuntimeType_GetCorrespondingInflatedMethod_raw,
// token 727,
ves_icall_RuntimeType_make_array_type_raw,
// token 730,
ves_icall_RuntimeType_make_byref_type_raw,
// token 732,
ves_icall_RuntimeType_make_pointer_type_raw,
// token 737,
ves_icall_RuntimeType_MakeGenericType_raw,
// token 738,
ves_icall_RuntimeType_GetMethodsByName_native_raw,
// token 740,
ves_icall_RuntimeType_GetPropertiesByName_native_raw,
// token 741,
ves_icall_RuntimeType_GetConstructors_native_raw,
// token 745,
ves_icall_System_RuntimeType_CreateInstanceInternal_raw,
// token 746,
ves_icall_System_RuntimeType_AllocateValueType_raw,
// token 748,
ves_icall_RuntimeType_GetDeclaringMethod_raw,
// token 750,
ves_icall_System_RuntimeType_getFullName_raw,
// token 751,
ves_icall_RuntimeType_GetGenericArgumentsInternal_raw,
// token 754,
ves_icall_RuntimeType_GetGenericParameterPosition,
// token 755,
ves_icall_RuntimeType_GetEvents_native_raw,
// token 756,
ves_icall_RuntimeType_GetFields_native_raw,
// token 759,
ves_icall_RuntimeType_GetInterfaces_raw,
// token 761,
ves_icall_RuntimeType_GetNestedTypes_native_raw,
// token 764,
ves_icall_RuntimeType_GetDeclaringType_raw,
// token 766,
ves_icall_RuntimeType_GetName_raw,
// token 768,
ves_icall_RuntimeType_GetNamespace_raw,
// token 777,
ves_icall_RuntimeType_FunctionPointerReturnAndParameterTypes_raw,
// token 841,
ves_icall_RuntimeTypeHandle_GetAttributes,
// token 843,
ves_icall_RuntimeTypeHandle_GetMetadataToken_raw,
// token 845,
ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl_raw,
// token 855,
ves_icall_RuntimeTypeHandle_GetCorElementType,
// token 856,
ves_icall_RuntimeTypeHandle_HasInstantiation,
// token 857,
ves_icall_RuntimeTypeHandle_IsInstanceOfType_raw,
// token 859,
ves_icall_RuntimeTypeHandle_HasReferences_raw,
// token 865,
ves_icall_RuntimeTypeHandle_GetArrayRank_raw,
// token 866,
ves_icall_RuntimeTypeHandle_GetAssembly_raw,
// token 867,
ves_icall_RuntimeTypeHandle_GetElementType_raw,
// token 868,
ves_icall_RuntimeTypeHandle_GetModule_raw,
// token 869,
ves_icall_RuntimeTypeHandle_GetBaseType_raw,
// token 877,
ves_icall_RuntimeTypeHandle_type_is_assignable_from_raw,
// token 878,
ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition,
// token 879,
ves_icall_RuntimeTypeHandle_GetGenericParameterInfo_raw,
// token 883,
ves_icall_RuntimeTypeHandle_is_subclass_of_raw,
// token 884,
ves_icall_RuntimeTypeHandle_IsByRefLike_raw,
// token 886,
ves_icall_System_RuntimeTypeHandle_internal_from_name_raw,
// token 888,
ves_icall_System_String_FastAllocateString_raw,
// token 1094,
ves_icall_System_Type_internal_from_handle_raw,
// token 1269,
ves_icall_System_ValueType_InternalGetHashCode_raw,
// token 1270,
ves_icall_System_ValueType_Equals_raw,
// token 7166,
ves_icall_System_Threading_Interlocked_CompareExchange_Int,
// token 7167,
ves_icall_System_Threading_Interlocked_CompareExchange_Object,
// token 7169,
ves_icall_System_Threading_Interlocked_Decrement_Int,
// token 7170,
ves_icall_System_Threading_Interlocked_Increment_Int,
// token 7171,
ves_icall_System_Threading_Interlocked_Increment_Long,
// token 7172,
ves_icall_System_Threading_Interlocked_Exchange_Int,
// token 7173,
ves_icall_System_Threading_Interlocked_Exchange_Object,
// token 7175,
ves_icall_System_Threading_Interlocked_CompareExchange_Long,
// token 7177,
ves_icall_System_Threading_Interlocked_Exchange_Long,
// token 7179,
ves_icall_System_Threading_Interlocked_Add_Int,
// token 7180,
ves_icall_System_Threading_Interlocked_Add_Long,
// token 7190,
ves_icall_System_Threading_Monitor_Monitor_Enter_raw,
// token 7192,
mono_monitor_exit_icall_raw,
// token 7197,
ves_icall_System_Threading_Monitor_Monitor_pulse_raw,
// token 7199,
ves_icall_System_Threading_Monitor_Monitor_pulse_all_raw,
// token 7201,
ves_icall_System_Threading_Monitor_Monitor_wait_raw,
// token 7203,
ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var_raw,
// token 7252,
ves_icall_System_Threading_Thread_StartInternal_raw,
// token 7258,
ves_icall_System_Threading_Thread_InitInternal_raw,
// token 7259,
ves_icall_System_Threading_Thread_GetCurrentThread,
// token 7261,
ves_icall_System_Threading_InternalThread_Thread_free_internal_raw,
// token 7262,
ves_icall_System_Threading_Thread_GetState_raw,
// token 7263,
ves_icall_System_Threading_Thread_SetState_raw,
// token 7264,
ves_icall_System_Threading_Thread_ClrState_raw,
// token 7265,
ves_icall_System_Threading_Thread_SetName_icall_raw,
// token 7267,
ves_icall_System_Threading_Thread_YieldInternal,
// token 7269,
ves_icall_System_Threading_Thread_SetPriority_raw,
// token 8323,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease_raw,
// token 8327,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly_raw,
// token 8329,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile_raw,
// token 8330,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC_raw,
// token 8331,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream_raw,
// token 8332,
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies_raw,
// token 8581,
ves_icall_System_GCHandle_InternalAlloc_raw,
// token 8582,
ves_icall_System_GCHandle_InternalFree_raw,
// token 8583,
ves_icall_System_GCHandle_InternalGet_raw,
// token 8584,
ves_icall_System_GCHandle_InternalSet_raw,
// token 8602,
ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError,
// token 8603,
ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError,
// token 8604,
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr_raw,
// token 8606,
ves_icall_System_Runtime_InteropServices_Marshal_SizeOfHelper_raw,
// token 8654,
ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName_raw,
// token 8732,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalGetHashCode_raw,
// token 8734,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalTryGetHashCode_raw,
// token 8736,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue_raw,
// token 8745,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal_raw,
// token 8746,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray_raw,
// token 8747,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetSpanDataFrom_raw,
// token 8748,
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack,
// token 9192,
ves_icall_System_Reflection_Assembly_GetEntryAssembly_raw,
// token 9196,
ves_icall_System_Reflection_Assembly_InternalLoad_raw,
// token 9197,
ves_icall_System_Reflection_Assembly_InternalGetType_raw,
// token 9224,
ves_icall_System_Reflection_AssemblyName_GetNativeName,
// token 9242,
ves_icall_MonoCustomAttrs_GetCustomAttributesInternal_raw,
// token 9249,
ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal_raw,
// token 9256,
ves_icall_MonoCustomAttrs_IsDefinedInternal_raw,
// token 9267,
ves_icall_System_Reflection_FieldInfo_internal_from_handle_type_raw,
// token 9271,
ves_icall_System_Reflection_FieldInfo_get_marshal_info_raw,
// token 9291,
ves_icall_System_Reflection_LoaderAllocatorScout_Destroy,
// token 9364,
ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames_raw,
// token 9366,
ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes_raw,
// token 9375,
ves_icall_System_Reflection_RuntimeAssembly_GetInfo_raw,
// token 9377,
ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal_raw,
// token 9378,
ves_icall_System_Reflection_Assembly_GetManifestModuleInternal_raw,
// token 9385,
ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal_raw,
// token 9399,
ves_icall_RuntimeEventInfo_get_event_info_raw,
// token 9419,
ves_icall_reflection_get_token_raw,
// token 9420,
ves_icall_System_Reflection_EventInfo_internal_from_handle_type_raw,
// token 9428,
ves_icall_RuntimeFieldInfo_ResolveType_raw,
// token 9430,
ves_icall_RuntimeFieldInfo_GetParentType_raw,
// token 9437,
ves_icall_RuntimeFieldInfo_GetFieldOffset_raw,
// token 9438,
ves_icall_RuntimeFieldInfo_GetValueInternal_raw,
// token 9441,
ves_icall_RuntimeFieldInfo_SetValueInternal_raw,
// token 9443,
ves_icall_RuntimeFieldInfo_GetRawConstantValue_raw,
// token 9448,
ves_icall_reflection_get_token_raw,
// token 9454,
ves_icall_get_method_info_raw,
// token 9455,
ves_icall_get_method_attributes,
// token 9462,
ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info_raw,
// token 9464,
ves_icall_System_MonoMethodInfo_get_retval_marshal_raw,
// token 9476,
ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native_raw,
// token 9479,
ves_icall_RuntimeMethodInfo_get_name_raw,
// token 9480,
ves_icall_RuntimeMethodInfo_get_base_method_raw,
// token 9481,
ves_icall_reflection_get_token_raw,
// token 9492,
ves_icall_InternalInvoke_raw,
// token 9501,
ves_icall_RuntimeMethodInfo_GetPInvoke_raw,
// token 9507,
ves_icall_RuntimeMethodInfo_MakeGenericMethod_impl_raw,
// token 9508,
ves_icall_RuntimeMethodInfo_GetGenericArguments_raw,
// token 9509,
ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition_raw,
// token 9511,
ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition_raw,
// token 9512,
ves_icall_RuntimeMethodInfo_get_IsGenericMethod_raw,
// token 9529,
ves_icall_InvokeClassConstructor_raw,
// token 9531,
ves_icall_InternalInvoke_raw,
// token 9545,
ves_icall_reflection_get_token_raw,
// token 9562,
ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken_raw,
// token 9589,
ves_icall_RuntimePropertyInfo_get_property_info_raw,
// token 9619,
ves_icall_reflection_get_token_raw,
// token 9620,
ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type_raw,
// token 10103,
ves_icall_DynamicMethod_create_dynamic_method_raw,
// token 10195,
ves_icall_AssemblyBuilder_basic_init_raw,
// token 10196,
ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes_raw,
// token 10396,
ves_icall_ModuleBuilder_basic_init_raw,
// token 10397,
ves_icall_ModuleBuilder_set_wrappers_type_raw,
// token 10404,
ves_icall_ModuleBuilder_getUSIndex_raw,
// token 10405,
ves_icall_ModuleBuilder_getToken_raw,
// token 10406,
ves_icall_ModuleBuilder_getMethodToken_raw,
// token 10411,
ves_icall_ModuleBuilder_RegisterToken_raw,
// token 10486,
ves_icall_TypeBuilder_create_runtime_class_raw,
// token 10952,
ves_icall_System_IO_Stream_HasOverriddenBeginEndRead_raw,
// token 10953,
ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite_raw,
// token 11322,
ves_icall_System_Diagnostics_Debugger_IsAttached_internal,
// token 11323,
ves_icall_System_Diagnostics_Debugger_IsLogging,
// token 11324,
ves_icall_System_Diagnostics_Debugger_Log,
// token 11329,
ves_icall_System_Diagnostics_StackFrame_GetFrameInfo,
// token 11339,
ves_icall_System_Diagnostics_StackTrace_GetTrace,
// token 12175,
ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass,
// token 12196,
ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree,
// token 12198,
ves_icall_Mono_SafeStringMarshal_StringToUtf8,
// token 12200,
ves_icall_Mono_SafeStringMarshal_GFree,
};
static uint8_t corlib_icall_flags [] = {
0,
0,
0,
0,
0,
4,
4,
0,
4,
0,
4,
4,
0,
0,
0,
4,
4,
4,
4,
4,
0,
4,
0,
0,
0,
4,
4,
4,
4,
4,
0,
4,
4,
0,
0,
0,
0,
0,
0,
0,
0,
0,
0,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
0,
0,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
4,
4,
4,
0,
0,
0,
0,
0,
0,
0,
0,
0,
0,
0,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
0,
0,
4,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
4,
0,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
0,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
4,
0,
0,
0,
0,
0,
0,
0,
0,
0,
};
