using System;
using System.Collections.Generic;

namespace HMSX.Second.Plugin.Tool
{
    public static class EIEnumerable
    {
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