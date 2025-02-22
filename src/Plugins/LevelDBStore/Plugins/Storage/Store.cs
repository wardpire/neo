// Copyright (C) 2015-2025 The Neo Project.
//
// Store.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

//using Neo.IO.Data.LevelDB;
using Akka.Util;
using LevelDB;
using LevelDB.NativePointer;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Plugins.Storage
{
    /// <summary>
    /// <code>Iterating over the whole dataset can be time-consuming. Depending upon how large the dataset is.</code>
    /// </summary>
    internal class Store : IStore, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        private readonly DB _db;
        private readonly Options _options;

        public Store(string path)
        {
            _options = new Options { CreateIfMissing = true };
            _db = new DB(_options, path);
        }

        public void Delete(byte[] key)
        {
            _db.Delete(key);
        }

        public void Dispose()
        {
            _db.Dispose();
            _options.Dispose();
        }

        public IEnumerable<(byte[], byte[])> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            var it = _db.CreateIterator(new ReadOptions());
            if (direction == SeekDirection.Forward)
            {
                for (it.Seek(prefix); it.IsValid(); it.Next())
                    yield return (it.Key(), it.Value());
            }
            else
            {
                // SeekForPrev
                it.Seek(prefix);
                if (!it.IsValid())
                    it.SeekToLast();
                else if (it.Key().AsSpan().SequenceCompareTo(prefix) > 0)
                    it.Prev();

                for (; it.IsValid(); it.Prev())
                    yield return (it.Key(), it.Value());
            }
        }

        public ISnapshot GetSnapshot()
        {
            return new Snapshot(_db);
        }

        public void Put(byte[] key, byte[] value)
        {
            _db.Put(key, value);
        }

        public void PutSync(byte[] key, byte[] value)
        {
            _db.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            var val = _db.Get(key);
            return val != null && val.Length > 0;
        }

        public byte[] TryGet(byte[] key)
        {
            return _db.Get(key);
        }

        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            value = _db.Get(key);
            return value != null && value.Length > 0;
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            return _db.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _db.GetEnumerator();
        }
    }
}
