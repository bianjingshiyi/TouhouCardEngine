using System;

namespace TouhouCardEngine
{
    public static class FlowConvert
    {
        /// <summary>
        /// is object nessesary to convert to target array type?
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool isObjectNeedToPackForType(object obj, Type type)
        {
            return !(obj is Array) && //值不是数组
                (type == typeof(Array) || type.IsArray); //类型是数组
        }
        public static bool isObjectNeedToUnpackForType(object obj, Type type)
        {
            return obj is Array && //值是数组
                !(type == typeof(Array) || type.IsArray || type.IsAssignableFrom(obj.GetType())); //类型不是数组
        }
        /// <summary>
        /// 是否需要将数组转换为目标类型的数组？
        /// </summary>
        /// <param name="array">要转换的数组。</param>
        /// <param name="type">目标类型。</param>
        /// <returns></returns>
        public static bool isArrayNeedToCastForType(Array array, Type type)
        {
            if (array == null) // 数组为null。
                return false;
            if (type != typeof(Array) && !type.IsArray) // 类型不是数组。
                return false;
            if (!type.HasElementType)// 目标类型数组没有元素类型。
                return false;

            Type arrayType = array.GetType();
            if (type.IsAssignableFrom(arrayType)) // 该数组可以直接转换到目标数组的类型。
                return false;

            Type arrayElementType = arrayType.GetElementType();
            Type elementType = type.GetElementType();
            return Flow.canConvertTo(arrayElementType, elementType) || Flow.canConvertTo(elementType, arrayElementType); // 目标类型数组的元素可以和数组的元素类型相转换。
        }
        public static object packObjectToArray(object obj, Type elementType)
        {
            if (obj == null)
                return Array.CreateInstance(elementType, 0);
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(obj, 0);
            return array;
        }
        public static object unpackArrayToObject(Array array)
        {
            if (array == null || array.Length <= 0)
                return null;
            return array.GetValue(0);
        }
        public static object castArrayToTargetTypeArray(Flow flow, Array array, Type elementType)
        {
            Array targetArray = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                var value = array.GetValue(i);
                value = flow.convertTo(value, elementType);
                targetArray.SetValue(value, i);
            }
            return targetArray;
        }
        public static object convertType(Flow flow, object param, Type type)
        {
            if (flow.tryConvertTo(param, type, out var output))
            {
                return output;
            }

            if (isObjectNeedToPackForType(param, type))
                param = packObjectToArray(param, type.GetElementType());
            else if (isObjectNeedToUnpackForType(param, type))
                param = unpackArrayToObject(param as Array);
            else if (param is Array array && isArrayNeedToCastForType(array, type))
                param = castArrayToTargetTypeArray(flow, array, type.GetElementType());
            return param;
        }
        public static bool tryConvertType<T>(Flow flow, object param, out T output)
        {
            if (convertType(flow, param, typeof(T)) is T tValue)
            {
                output = tValue;
                return true;
            }
            output = default;
            return false;
        }
    }
}
