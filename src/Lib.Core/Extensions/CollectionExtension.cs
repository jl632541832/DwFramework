﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DwFramework.Core.Extensions
{
    public static class CollectionExtension
    {
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable) action(item);
        }
    }
}
