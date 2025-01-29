// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_Blockchain : TestKit
    {
        private NeoSystem system;
        private Transaction txSample;
        private TestProbe senderProbe;

        [TestInitialize]
        public void Initialize()
        {
            system = TestBlockchain.TheNeoSystem;
            senderProbe = CreateTestProbe();
            txSample = new Transaction
            {
                Attributes = [],
                Script = Array.Empty<byte>(),
                Signers = [new Signer { Account = UInt160.Zero }],
                Witnesses = []
            };
            system.MemPool.TryAdd(txSample, TestBlockchain.GetTestSnapshotCache());
        }

        [TestCleanup]
        public void Clean()
        {
            TestBlockchain.ResetStore();
        }

        [TestMethod]
        public void TestValidTransaction()
        {
            var snapshot = TestBlockchain.TheNeoSystem.GetSnapshotCache();
            var walletA = TestUtils.GenerateTestWallet("123");
            var acc = walletA.CreateAccount();

            // Fake balance

            var key = new KeyBuilder(TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Id, 20).Add(acc.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Factor;
            snapshot.Commit();

            // Make transaction

            var tx = TestUtils.CreateValidTx(snapshot, walletA, acc.ScriptHash, 0);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.AlreadyInPool);
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = TestBlockchain.TheNeoSystem.NativeContractRepository.NEO.Id,
                Key = buffer
            };
        }


        [TestMethod]
        public void TestMaliciousOnChainConflict()
        {
            var snapshot = TestBlockchain.TheNeoSystem.GetSnapshotCache();
            var walletA = TestUtils.GenerateTestWallet("123");
            var accA = walletA.CreateAccount();
            var walletB = TestUtils.GenerateTestWallet("456");
            var accB = walletB.CreateAccount();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            engine.LoadScript(Array.Empty<byte>());

            // Fake balance for accounts A and B.
            var key = new KeyBuilder(TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Id, 20).Add(accA.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Factor;
            snapshot.Commit();

            key = new KeyBuilder(TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Id, 20).Add(accB.ScriptHash);
            entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Factor;
            snapshot.Commit();

            // Create transactions:
            //    tx1 conflicts with tx2 and has the same sender (thus, it's a valid conflict and must prevent tx2 from entering the chain);
            //    tx2 conflicts with tx3 and has different sender (thus, this conflict is invalid and must not prevent tx3 from entering the chain).
            var tx1 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 0);
            var tx2 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 1);
            var tx3 = TestUtils.CreateValidTx(snapshot, walletB, accB.ScriptHash, 2);

            tx1.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = tx2.Hash }, new Conflicts() { Hash = tx3.Hash } };

            // Persist tx1.
            var block = new Block
            {
                Header = new Header()
                {
                    Index = 5, // allow tx1, tx2 and tx3 to fit into MaxValidUntilBlockIncrement.
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx1 },
            };
            byte[] onPersistScript;
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
                onPersistScript = sb.ToArray();
            }
            using (ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, block, TestBlockchain.TheNeoSystem.Settings, 0))
            {
                engine2.LoadScript(onPersistScript);
                if (engine2.Execute() != VMState.HALT) throw engine2.FaultException;
                engine2.SnapshotCache.Commit();
            }
            snapshot.Commit();

            // Run PostPersist to update current block index in native Ledger.
            // Relevant current block index is needed for conflict records checks.
            byte[] postPersistScript;
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                postPersistScript = sb.ToArray();
            }
            using (ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, TestBlockchain.TheNeoSystem.NativeContractRepository, block, TestBlockchain.TheNeoSystem.Settings, 0))
            {
                engine2.LoadScript(postPersistScript);
                if (engine2.Execute() != VMState.HALT) throw engine2.FaultException;
                engine2.SnapshotCache.Commit();
            }
            snapshot.Commit();

            // Add tx2: must fail because valid conflict is alredy on chain (tx1).
            senderProbe.Send(TestBlockchain.TheNeoSystem.Blockchain, tx2);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.HasConflicts);

            // Add tx3: must succeed because on-chain conflict is invalid (doesn't have proper signer).
            senderProbe.Send(TestBlockchain.TheNeoSystem.Blockchain, tx3);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed);
        }
    }
}
