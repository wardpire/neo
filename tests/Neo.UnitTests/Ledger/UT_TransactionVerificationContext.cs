// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TransactionVerificationContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionVerificationContext
    {
        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            _ = TestBlockchain.TheNeoSystem;
        }

        private Transaction CreateTransactionWithFee(long networkFee, long systemFee)
        {
            Random random = new();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new();
            mock.Setup(p => p.VerifyStateDependent(It.IsAny<ProtocolSettings>(), It.IsAny<ClonedCache>(), It.IsAny<TransactionVerificationContext>(), TestBlockchain.TheNeoSystem.NativeContractRepository, It.IsAny<IEnumerable<Transaction>>())).Returns(VerifyResult.Succeed);
            mock.Setup(p => p.VerifyStateIndependent(It.IsAny<ProtocolSettings>(), TestBlockchain.TheNeoSystem.NativeContractRepository)).Returns(VerifyResult.Succeed);
            mock.Object.Script = randomBytes;
            mock.Object.NetworkFee = networkFee;
            mock.Object.SystemFee = systemFee;
            mock.Object.Signers = new[] { new Signer { Account = UInt160.Zero } };
            mock.Object.Attributes = Array.Empty<TransactionAttribute>();
            mock.Object.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };
            return mock.Object;
        }

        [TestMethod]
        public async Task TestDuplicateOracle()
        {
            // Fake balance
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshotCache, UInt160.Zero);
            await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, UInt160.Zero, balance);
            _ = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Mint(engine, UInt160.Zero, 8, false);

            // Test
            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() } };
            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);

            tx = CreateTransactionWithFee(2, 1);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() } };
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeFalse();
        }

        [TestMethod]
        public async Task TestTransactionSenderFee()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshotCache, UInt160.Zero);
            await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, UInt160.Zero, balance);
            _ = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Mint(engine, UInt160.Zero, 8, true);

            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeFalse();
            verificationContext.RemoveTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeFalse();
        }

        [TestMethod]
        public async Task TestTransactionSenderFeeWithConflicts()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.BalanceOf(snapshotCache, UInt160.Zero);
            await TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Burn(engine, UInt160.Zero, balance);
            _ = TestBlockchain.TheNeoSystem.NativeContractRepository.GAS.Mint(engine, UInt160.Zero, 3 + 3 + 1, true); // balance is enough for 2 transactions and 1 GAS is left.

            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            var conflictingTx = CreateTransactionWithFee(1, 1); // costs 2 GAS

            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeFalse();

            conflicts.Add(conflictingTx);
            verificationContext.CheckTransaction(tx, conflicts, snapshotCache, TestBlockchain.TheNeoSystem.NativeContractRepository).Should().BeTrue(); // 1 GAS is left on the balance + 2 GAS is free after conflicts removal => enough for one more trasnaction.
        }
    }
}
