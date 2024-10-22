// Copyright (C) 2015-2024 The Neo Project.
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
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    internal class Store : IStore
    {
        private readonly DB db;

        public Store(string path)
        {
            var options = new Options { CreateIfMissing = true };
            db = new DB(options, path);
        }

        public void Delete(byte[] key)
        {
            db.Delete(key);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public IEnumerable<(byte[], byte[])> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            var it = db.CreateIterator(new ReadOptions());
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
            return new Snapshot(db);
        }

        public void Put(byte[] key, byte[] value)
        {
            db.Put(key, value);
        }

        public void PutSync(byte[] key, byte[] value)
        {
            db.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            var val = db.Get(key);
            return val != null && val.Length > 0;
        }

        public byte[] TryGet(byte[] key)
        {
            return db.Get(key);
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = db.Get(key);
            return value != null && value.Length > 0;
        }
    }
}
