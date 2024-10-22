// Copyright (C) 2015-2024 The Neo Project.
//
// Snapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using LevelDB;
using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly LevelDB.DB _db;
        private readonly SnapShot _snapShot;
        private readonly LevelDB.WriteBatch _batch;

        public Snapshot(LevelDB.DB db)
        {
            _db = db;
            _snapShot = _db.CreateSnapshot();
            _batch = new LevelDB.WriteBatch();
        }

        public void Commit()
        {
            _db.Write(_batch);
            _batch.Clear();
        }

        public void Delete(byte[] key)
        {
            _batch.Delete(key);
        }

        public void Dispose()
        {
            _snapShot.Dispose();
            _batch.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            var it = _db.CreateIterator(new LevelDB.ReadOptions() { Snapshot = _snapShot });
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

        public void Put(byte[] key, byte[] value)
        {
            _batch.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            var val = TryGet(key);
            return val != null && val.Length > 0;
        }

        public byte[] TryGet(byte[] key)
        {
            return _db.Get(key, new LevelDB.ReadOptions() { Snapshot = _snapShot });
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = db.Get(options, key);
            return value != null;
        }
    }
}
