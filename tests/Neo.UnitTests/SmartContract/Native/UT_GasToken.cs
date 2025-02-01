// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GasToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_GasToken
    {
        private DataCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_Name() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Name.Should().Be(nameof(GasToken));

        [TestMethod]
        public void Check_Symbol() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Symbol(_snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().Be("GAS");

        [TestMethod]
        public void Check_Decimals() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Decimals(_snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().Be(8);

        [TestMethod]
        public async Task Check_BalanceOfTransferAndBurn()
        {
            var snapshot = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            byte[] to = new byte[20];
            var supply = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.TotalSupply(snapshot);
            supply.Should().Be(5200000050000000); // 3000000000000000 + 50000000 (neo holder reward)

            var storageKey = new KeyBuilder(TestBlockchain.TheNeoSystem.NativeContractRepository.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            var keyCount = snapshot.GetChangeSet().Count();
            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            unclaim.Value.Should().Be(new BigInteger(0.5 * 1000 * 100000000L));
            unclaim.State.Should().BeTrue();

            // Transfer

            TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, from, to, BigInteger.Zero, true, persistingBlock).Should().BeTrue();
            Assert.ThrowsException<ArgumentNullException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, from, null, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsException<ArgumentNullException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, null, to, BigInteger.Zero, false, persistingBlock));
            TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.BalanceOf(snapshot, from).Should().Be(100000000);
            TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.BalanceOf(snapshot, to).Should().Be(0);

            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshot, from).Should().Be(52000500_00000000);
            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshot, to).Should().Be(0);

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            supply = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.TotalSupply(snapshot);
            supply.Should().Be(5200050050000000);

            snapshot.GetChangeSet().Count().Should().Be(keyCount + 3); // Gas

            // Transfer

            keyCount = snapshot.GetChangeSet().Count();

            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, from, to, 52000500_00000000, false, persistingBlock).Should().BeFalse(); // Not signed
            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, from, to, 52000500_00000001, true, persistingBlock).Should().BeFalse(); // More than balance
            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, from, to, 52000500_00000000, true, persistingBlock).Should().BeTrue(); // All balance

            // Balance of

            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshot, to).Should().Be(52000500_00000000);
            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshot, from).Should().Be(0);

            snapshot.GetChangeSet().Count().Should().Be(keyCount + 1); // All

            // Burn

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 0);
            engine.LoadScript(Array.Empty<byte>());

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
                await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, new UInt160(to), BigInteger.MinusOne));

            // Burn more than expected

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, new UInt160(to), new BigInteger(52000500_00000001)));

            // Real burn

            await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, new UInt160(to), new BigInteger(1));

            TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(engine.SnapshotCache, to).Should().Be(5200049999999999);

            engine.SnapshotCache.GetChangeSet().Count().Should().Be(2);

            // Burn all
            await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, new UInt160(to), new BigInteger(5200049999999999));

            (keyCount - 2).Should().Be(engine.SnapshotCache.GetChangeSet().Count());

            // Bad inputs

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(engine.SnapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, from, to, BigInteger.MinusOne, true, persistingBlock));
            Assert.ThrowsException<FormatException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(engine.SnapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, new byte[19], to, BigInteger.One, false, persistingBlock));
            Assert.ThrowsException<FormatException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Transfer(engine.SnapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, from, new byte[19], BigInteger.One, false, persistingBlock));
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Id,
                Key = buffer
            };
        }
    }
}
