// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Syscalls.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using Array = System.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_Syscalls : TestKit
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void System_Blockchain_GetBlock()
        {
            var tx = new Transaction()
            {
                Script = new byte[] { 0x01 },
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = Array.Empty<Signer>(),
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
            };

            var block = new TrimmedBlock()
            {
                Header = new Header
                {
                    Index = 0,
                    Timestamp = 2,
                    Witness = new Witness()
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    },
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    PrimaryIndex = 1,
                    NextConsensus = UInt160.Zero,
                },
                Hashes = new[] { tx.Hash }
            };

            var snapshot = _snapshotCache.CloneCache();

            using ScriptBuilder script = new();
            script.EmitDynamicCall(TestBlockchain.TheNeoSystem.NativeContractRepository.Ledger.Hash, "getBlock", block.Hash.ToArray());

            // Without block

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.IsTrue(engine.ResultStack.Peek().IsNull);

            // Not traceable block

            const byte Prefix_Transaction = 11;
            const byte Prefix_CurrentBlock = 12;

            TestUtils.BlocksAdd(snapshot, block.Hash, block);

            var height = snapshot[TestBlockchain.TheNeoSystem.NativeContractRepository.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
            height.Index = block.Index + TestProtocolSettings.Default.MaxTraceableBlocks;

            snapshot.Add(TestBlockchain.TheNeoSystem.NativeContractRepository.Ledger.CreateStorageKey(Prefix_Transaction, tx.Hash), new StorageItem(new TransactionState
            {
                BlockIndex = block.Index,
                Transaction = tx
            }));

            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.IsTrue(engine.ResultStack.Peek().IsNull);

            // With block

            height = snapshot[TestBlockchain.TheNeoSystem.NativeContractRepository.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
            height.Index = block.Index;

            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var array = engine.ResultStack.Pop<VM.Types.Array>();
            Assert.AreEqual(block.Hash, new UInt256(array[0].GetSpan()));
        }

        [TestMethod]
        public void System_ExecutionEngine_GetScriptContainer()
        {
            var snapshot = _snapshotCache.CloneCache();
            using ScriptBuilder script = new();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetScriptContainer);

            // Without tx

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // With tx

            var tx = new Transaction()
            {
                Script = new byte[] { 0x01 },
                Signers = new Signer[] {
                    new Signer()
                    {
                        Account = UInt160.Zero,
                        Scopes = WitnessScope.None,
                        AllowedContracts = Array.Empty<UInt160>(),
                        AllowedGroups = Array.Empty<ECPoint>(),
                        Rules = Array.Empty<WitnessRule>(),
                    }
                },
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
            };

            engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var array = engine.ResultStack.Pop<VM.Types.Array>();
            Assert.AreEqual(tx.Hash, new UInt256(array[0].GetSpan()));
        }

        [TestMethod]
        public void System_Runtime_GasLeft()
        {
            var snapshot = _snapshotCache.CloneCache();

            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, gas: 100_000_000);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(engine.Execute(), VMState.HALT);

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[] { 99_999_490, 99_998_980, 99_998_410 }
                    );
            }

            // Check test mode

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository);
                engine.LoadScript(script.ToArray());

                // Check the results

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(Integer));
                Assert.AreEqual(1999999520, engine.ResultStack.Pop().GetInteger());
            }
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            var snapshot = _snapshotCache.CloneCache();
            ContractState contractA, contractB, contractC;

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetInvocationCounter);

                contractA = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray());
                contractB = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray());
                contractC = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray());
                contractA.Hash = contractA.Script.Span.ToScriptHash();
                contractB.Hash = contractB.Script.Span.ToScriptHash();
                contractC.Hash = contractC.Script.Span.ToScriptHash();

                // Init A,B,C contracts
                // First two drops is for drop method and arguments

                snapshot.DeleteContract(contractA.Hash, TestBlockchain.TheNeoSystem.NativeContractRepository);
                snapshot.DeleteContract(contractB.Hash, TestBlockchain.TheNeoSystem.NativeContractRepository);
                snapshot.DeleteContract(contractC.Hash, TestBlockchain.TheNeoSystem.NativeContractRepository);
                contractA.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                contractB.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                contractC.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                snapshot.AddContract(contractA.Hash, contractA, TestBlockchain.TheNeoSystem.NativeContractRepository);
                snapshot.AddContract(contractB.Hash, contractB, TestBlockchain.TheNeoSystem.NativeContractRepository);
                snapshot.AddContract(contractC.Hash, contractC, TestBlockchain.TheNeoSystem.NativeContractRepository);
            }

            // Call A,B,B,C

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(contractA.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractC.Hash, "dummyMain", "0", 1);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, null, ProtocolSettings.Default);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[]
                        {
                        1, /* A */
                        1, /* B */
                        2, /* B */
                        1  /* C */
                        }
                    );
            }
        }
    }
}
