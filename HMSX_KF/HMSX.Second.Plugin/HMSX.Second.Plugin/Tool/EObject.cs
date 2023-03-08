using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.Tool
{
    public static class EObject
    {
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="obj">需要转换的值</param>
        /// <param name="value">默认值</param>
        /// <returns></returns>
        public static T TypeCast<T>(this object obj, T value = default)
        {
            T instance;
            if (null == obj)
            {
                instance = value;
            }
            else
            {
                try
                {
                    if (!typeof(T).IsGenericType)
                    {
                        instance = (T)Convert.ChangeType(obj, typeof(T));
                    }
                    else if (typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        instance = (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(typeof(T)));
                    }
                    else
                    {
                        instance = (T)Convert.ChangeType(obj, typeof(T));
                    }
                }
                catch (Exception)
                {
                    instance = value;
                }
            }

            return instance;
        }
        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <typeparam name="T">需要返回的值类型</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(this object obj)
        {
            var X = obj.GetType();
            var o = Activator.CreateInstance(X);
            var PI = X.GetProperties();
            for (int i = 0; i < PI.Length; i++)
            {
                var P = PI[i];
                // P.SetValue(o, P.GetValue(obj));
            }

            return o.TypeCast(default(T));
        }

        /// <summary>
        /// foreach循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ie"></param>
        /// <param name="action">item 当前循环值, index 下标 返回false则中断执行,相当于break</param>
        public static bool ForEach<T>(this IEnumerable<T> ie, Func<T, int, bool> action)
        {
            var i = 0;
            foreach (T e in ie)
            {
                if (!action(e, i++))
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// foreach循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ie"></param>
        /// <param name="action">item 当前循环值, index 下标</param>
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (T e in ie)
            {
                action(e, i++);
            }
        }
    }
}

