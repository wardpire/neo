// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PolicyContract.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_PolicyContract
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, null, _snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, new Block { Header = new Header() }, settings: TestBlockchain.TheNeoSystem.Settings, gas: 0);
            TestBlockchain.TheNeoSystem.NativeContractRepository.ContractManagement.OnPersistAsync(engine);
        }

        [TestMethod]
        public void Check_Default()
        {
            var snapshot = _snapshotCache.CloneCache();

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1000);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts });
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(PolicyContract.DefaultAttributeFee);

            Assert.ThrowsException<InvalidOperationException>(() => TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)byte.MaxValue }));
        }

        [TestMethod]
        public void Check_SetAttributeFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain
            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            var attr = new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts };

            // Without signature
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(0);

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 11_0000_0000 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(0);

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            ret.IsNull.Should().BeTrue();

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(300300);

            // Set to zero
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            ret.IsNull.Should().BeTrue();

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(0);
        }

        [TestMethod]
        public void Check_SetFeePerByte()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            });

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1000);

            // With signature
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.IsNull.Should().BeTrue();

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_SetBaseExecFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            });

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(30);

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(30);

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            ret.IsNull.Should().BeTrue();

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(50);
        }

        [TestMethod]
        public void Check_SetStoragePrice()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(100000);

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(100000);

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            ret.IsNull.Should().BeTrue();

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository);
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(300300);
        }

        [TestMethod]
        public void Check_BlockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };

            // Without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            });

            // With signature

            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
              "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
              new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            // Same account
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            // Account B

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            // Check

            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();
            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")).Should().BeTrue();
            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")).Should().BeTrue();
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    Index = 1000,
                    PrevHash = UInt256.Zero
                }
            };
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);

            // Block without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();

            // Block with signature

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeTrue();

            // Unblock without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "unblockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeTrue();

            // Unblock with signature

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "unblockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();
        }
    }
}
