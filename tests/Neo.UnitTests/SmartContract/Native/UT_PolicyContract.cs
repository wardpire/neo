// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PolicyContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts });
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(PolicyContract.DefaultAttributeFee, ret.GetInteger());

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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 11_0000_0000 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(300300, ret.GetInteger());

            // Set to zero
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr, new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            Assert.IsTrue(ret.IsNull);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getAttributeFee", TestBlockchain.TheNeoSystem.NativeContractRepository, attr);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(0, ret.GetInteger());
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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            // With signature
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            Assert.IsTrue(ret.IsNull);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getFeePerByte", TestBlockchain.TheNeoSystem.NativeContractRepository);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(1, ret.GetInteger());
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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            Assert.IsTrue(ret.IsNull);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getExecFeeFactor", TestBlockchain.TheNeoSystem.NativeContractRepository);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(50, ret.GetInteger());
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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // Proper set
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, "getStoragePrice", TestBlockchain.TheNeoSystem.NativeContractRepository);
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Integer));
            Assert.AreEqual(300300, ret.GetInteger());
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
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Same account
            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Boolean));
            Assert.IsFalse(ret.GetBoolean());

            // Account B

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository,
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Check

            Assert.IsFalse(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero));
            Assert.IsTrue(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
            Assert.IsTrue(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
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

            Assert.IsFalse(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Block with signature

            var ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsTrue(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock without signature

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "unblockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            Assert.IsTrue(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock with signature

            ret = TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "unblockAccount", TestBlockchain.TheNeoSystem.NativeContractRepository, new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(VM.Types.Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsFalse(TestBlockchain.TheNeoSystem.NativeContractRepository.Policy.IsBlocked(snapshot, UInt160.Zero));
        }
    }
}
