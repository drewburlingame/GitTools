using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace BbGit.Tables
{
    public static class TypeExtensions
    {
        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            this Expression<Func<TSource, TProperty>> propertyLambda)
        {
            // https://stackoverflow.com/a/672212/169336
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            if (propInfo.ReflectedType != null 
                && type != propInfo.ReflectedType 
                && !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a property that is not from type {type}.");

            return propInfo;
        }

        public static bool IsNumeric(this Type t)
        {
            var type = t.GetTypeWithoutNullability();
            return
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(decimal) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(byte) ||
                type == typeof(sbyte);
        }

        public static Type GetTypeWithoutNullability(this Type t) => 
            t.IsNullable() 
                ? new NullableConverter(t).UnderlyingType 
                : t;

        public static bool IsNullable(this Type t) =>
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}