﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Vostok.Logging.Abstractions
{
    [PublicAPI]
    public static class WithPropertyLogExtensions
    {
        /// <summary>
        /// <para>Returns a wrapper log that adds a static property with given <paramref name="key"/> and <paramref name="value"/> to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// </summary>
        [Pure]
        public static ILog WithProperty<T>(this ILog log, string key, T value, bool allowOverwrite = false)
        {
            return new WithPropertyLog<T>(log, key, () => value, allowOverwrite, true);
        }

        /// <summary>
        /// <para>Returns a wrapper log that adds a dynamic property with given <paramref name="key"/> and <paramref name="value"/> provider to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// <para>By default, <c>null</c> values are not added to events. This can be changed via <paramref name="allowNullValues"/> parameter.</para>
        /// </summary>
        [Pure]
        public static ILog WithProperty<T>(this ILog log, string key, Func<T> value, bool allowOverwrite = false, bool allowNullValues = false)
        {
            return new WithPropertyLog<T>(log, key, value, allowOverwrite, allowNullValues);
        }

        /// <summary>
        /// <para>Returns a wrapper log that adds all of the given properties to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// <para>By default, <c>null</c> values are not added to events. This can be changed via <paramref name="allowNullValues"/> parameter.</para>
        /// </summary>
        [Pure]
        public static ILog WithProperties(this ILog log, IReadOnlyDictionary<string, object> properties, bool allowOverwrite = false, bool allowNullValues = false)
        {
            return new WithPropertiesLog(log, () => properties?.Select(pair => (pair.Key, pair.Value)), allowOverwrite, allowNullValues);
        }

        /// <summary>
        /// <para>Returns a wrapper log that adds all of the properties returned by given delegate to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// <para>By default, <c>null</c> values are not added to events. This can be changed via <paramref name="allowNullValues"/> parameter.</para>
        /// </summary>
        [Pure]
        public static ILog WithProperties(this ILog log, Func<IEnumerable<(string, object)>> properties, bool allowOverwrite = false, bool allowNullValues = false)
        {
            return new WithPropertiesLog(log, properties, allowOverwrite, allowNullValues);
        }

        /// <summary>
        /// <para>Returns a wrapper log that adds all properties of given <paramref name="@object"/> to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// <para>By default, <c>null</c> values are not added to events. This can be changed via <paramref name="allowNullValues"/> parameter.</para>
        /// <para>Usage example: <c>log.WithObjectProperties(new {A = 1, B = 2})</c></para>
        /// </summary>
        [Pure]
        public static ILog WithObjectProperties<T>(this ILog log, T @object, bool allowOverwrite = false, bool allowNullValues = false)
        {
            return new WithObjectPropertiesLog<T>(log, () => @object, allowOverwrite, allowNullValues);
        }

        /// <summary>
        /// <para>Returns a wrapper log that adds all properties of the object returned by given <paramref name="@object"/> delegate to each <see cref="LogEvent"/> before logging.</para>
        /// <para>By default, existing properties are not overwritten. This can be changed via <paramref name="allowOverwrite"/> parameter.</para>
        /// <para>By default, <c>null</c> values are not added to events. This can be changed via <paramref name="allowNullValues"/> parameter.</para>
        /// <para>Usage example: <c>log.WithObjectProperties(() => new {A = 1, B = 2})</c></para>
        /// </summary>
        [Pure]
        public static ILog WithObjectProperties<T>(this ILog log, Func<T> @object, bool allowOverwrite = false, bool allowNullValues = false)
        {
            return new WithObjectPropertiesLog<T>(log, @object, allowOverwrite, allowNullValues);
        }

        private class WithPropertyLog<T> : ILog
        {
            private readonly ILog baseLog;
            private readonly string key;
            private readonly Func<T> value;
            private readonly bool allowOverwrite;
            private readonly bool allowNullValues;

            public WithPropertyLog(ILog baseLog, string key, Func<T> value, bool allowOverwrite, bool allowNullValues)
            {
                this.baseLog = baseLog ?? throw new ArgumentNullException(nameof(baseLog));
                this.key = key ?? throw new ArgumentNullException(nameof(key));
                this.value = value;
                this.allowOverwrite = allowOverwrite;
                this.allowNullValues = allowNullValues;
            }

            public void Log(LogEvent @event)
            {
                var valueObject = value();
                if (valueObject != null || allowNullValues)
                {
                    @event = @event?.WithProperty(key, valueObject, allowOverwrite);
                }

                baseLog.Log(@event);
            }

            public bool IsEnabledFor(LogLevel level)
            {
                return baseLog.IsEnabledFor(level);
            }

            public ILog ForContext(string context)
            {
                var baseLogForContext = baseLog.ForContext(context);
                return ReferenceEquals(baseLogForContext, baseLog) ? this : new WithPropertyLog<T>(baseLogForContext, key, value, allowOverwrite, allowNullValues);
            }
        }

        private class WithPropertiesLog : ILog
        {
            private readonly ILog baseLog;
            private readonly Func<IEnumerable<(string, object)>> propertiesProvider;
            private readonly bool allowOverwrite;
            private readonly bool allowNullValues;

            public WithPropertiesLog(ILog baseLog, Func<IEnumerable<(string, object)>> propertiesProvider, bool allowOverwrite, bool allowNullValues)
            {
                this.baseLog = baseLog ?? throw new ArgumentNullException(nameof(baseLog));
                this.propertiesProvider = propertiesProvider ?? throw new ArgumentNullException(nameof(propertiesProvider));
                this.allowOverwrite = allowOverwrite;
                this.allowNullValues = allowNullValues;
            }

            public void Log(LogEvent @event)
            {
                foreach (var (key, value) in propertiesProvider() ?? Enumerable.Empty<(string, object)>())
                {
                    if (!allowNullValues && value == null)
                        continue;

                    @event = @event?.WithProperty(key, value, allowOverwrite);
                }

                baseLog.Log(@event);
            }

            public bool IsEnabledFor(LogLevel level)
            {
                return baseLog.IsEnabledFor(level);
            }

            public ILog ForContext(string context)
            {
                var baseLogForContext = baseLog.ForContext(context);
                return ReferenceEquals(baseLogForContext, baseLog) ? this : new WithPropertiesLog(baseLogForContext, propertiesProvider, allowOverwrite, allowNullValues);
            }
        }

        private class WithObjectPropertiesLog<T> : ILog
        {
            private readonly ILog baseLog;
            private readonly Func<T> objectProvider;
            private readonly bool allowOverwrite;
            private readonly bool allowNullValues;

            public WithObjectPropertiesLog(ILog baseLog, Func<T> objectProvider, bool allowOverwrite, bool allowNullValues)
            {
                this.baseLog = baseLog ?? throw new ArgumentNullException(nameof(baseLog));
                this.objectProvider = objectProvider ?? throw new ArgumentNullException(nameof(objectProvider));
                this.allowOverwrite = allowOverwrite;
                this.allowNullValues = allowNullValues;
            }

            public void Log(LogEvent @event)
            {
                var propertiesObject = objectProvider();
                if (propertiesObject != null)
                    @event = @event?.WithObjectProperties(propertiesObject, allowOverwrite, allowNullValues);

                baseLog.Log(@event);
            }

            public bool IsEnabledFor(LogLevel level)
            {
                return baseLog.IsEnabledFor(level);
            }

            public ILog ForContext(string context)
            {
                var baseLogForContext = baseLog.ForContext(context);
                return ReferenceEquals(baseLogForContext, baseLog) ? this : new WithObjectPropertiesLog<T>(baseLogForContext, objectProvider, allowOverwrite, allowNullValues);
            }
        }
    }
}
