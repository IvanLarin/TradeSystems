﻿using Newtonsoft.Json;
using System.Collections.Concurrent;
using TSLab.DataSource;
using TSLab.Script.Handlers;

namespace MegaTrade.Common.Caching;

public static class Cache
{
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    public static T Get<T>(string name, Func<T> calculate, object[] dependencies, IContext? ctx, CacheKind kind)
    {
        var key = MakeKey(name, dependencies);

        if (Load(out var data, kind)) return data!;

        var lockObject = Locks.GetOrAdd(key, _ => new object());

        lock (lockObject)
        {
            if (Load(out var data2, kind)) return data2!;

            var dt = DateTime.Now;

            ctx?.Log($"Начинаю считать {key}");

            var result = calculate();

            var ms = (int)((DateTime.Now - dt).TotalSeconds * 1000);
            ctx?.Log($"{ms}мс. {key}");

            Save(result, kind);

            return result;
        }

        bool Load(out T? output, CacheKind theKind)
        {
            output = default;

            if (theKind == CacheKind.Memory)
            {
                var fromCache = ctx?.LoadObject(key);
                if (fromCache is not NotClearableContainer<T> dataFromCache) return false;

                output = dataFromCache.Content;
                return true;
            }

            if (theKind == CacheKind.Disk)
            {
                var fromCache = ctx?.LoadObject(key, true);
                if (fromCache is not NotClearableContainer<T> dataFromCache) return false;

                output = dataFromCache.Content;
                return true;
            }

            if (Load(out var data3, CacheKind.Memory))
            {
                output = data3;
                return true;
            }

            if (Load(out var data4, CacheKind.Disk))
            {
                Save(data4!, CacheKind.Memory);
                output = data4;
                return true;
            }

            return false;
        }

        void Save(T value, CacheKind theKind)
        {
            switch (theKind)
            {
                case CacheKind.Memory:
                    ctx?.StoreObject(key, new NotClearableContainer<T>(value));
                    break;
                case CacheKind.Disk:
                    ctx?.StoreObject(key, new NotClearableContainer<T>(value), true);
                    break;
                case CacheKind.DiskAndMemory:
                    ctx?.StoreObject(key, new NotClearableContainer<T>(value));
                    ctx?.StoreObject(key, new NotClearableContainer<T>(value), true);
                    break;
                default: throw new NotImplementedException();
            }
        }
    }

    private static string MakeKey(string name, object[] dependencies) =>
        $"{name}: {JsonConvert.SerializeObject(dependencies)}";

    public static ICacheEntry<T> Entry<T>(string name, CacheKind kind, IContext? context, object[] dependencies) =>
        new CacheEntry<T>(name, dependencies, context, kind);
}