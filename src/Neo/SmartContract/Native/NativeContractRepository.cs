// Copyright (C) 2015-2024 The Neo Project.
//
// NativeContractRepository.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Neo.SmartContract.Native
{
    public class NativeContractRepository
    {
        private readonly Dictionary<UInt160, NativeContract> s_contractsDictionary = new();
        private int id_counter = 0;

        /// <summary>
        /// Gets all native contracts.
        /// </summary>
        public IReadOnlyCollection<NativeContract> Contracts { get => s_contractsDictionary.Values; }

        public NativeContractRepository()
        {
            new ContractManagement(this);
            new StdLib(this);
            new CryptoLib(this);
            new LedgerContract(this);
            new NeoToken(this);
            new GasToken(this);
            new PolicyContract(this);
            new RoleManagement(this);
            new OracleContract(this);
        }

        public int RegisterContract(UInt160 hash, NativeContract contract)
        {
            s_contractsDictionary.Add(hash, contract);
            return --id_counter;
        }

        /// <summary>
        /// Checks whether the committee has witnessed the current transaction.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing the contract.</param>
        /// <returns><see langword="true"/> if the committee has witnessed the current transaction; otherwise, <see langword="false"/>.</returns>
        public bool CheckCommittee(ApplicationEngine engine)
        {
            UInt160 committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.SnapshotCache);
            return engine.CheckWitnessInternal(committeeMultiSigAddr);
        }

        /// <summary>
        /// Gets the native contract with the specified hash.
        /// </summary>
        /// <param name="hash">The hash of the native contract.</param>
        /// <returns>The native contract with the specified hash.</returns>
        public NativeContract? GetContract(UInt160 hash)
        {
            s_contractsDictionary.TryGetValue(hash, out var contract);
            return contract;
        }

        /// <summary>
        /// Determine whether the specified contract is a native contract.
        /// </summary>
        /// <param name="hash">The hash of the contract.</param>
        /// <returns><see langword="true"/> if the contract is native; otherwise, <see langword="false"/>.</returns>
        public bool IsNative(UInt160 hash)
        {
            return s_contractsDictionary.ContainsKey(hash);
        }

        #region Named Native Contracts

        /// <summary>
        /// Gets the instance of the <see cref="Native.ContractManagement"/> class.
        /// </summary>
        public IContractManagement ContractManagement { get => GetNativeContract<IContractManagement>(); }

        /// <summary>
        /// Gets the instance of the <see cref="Native.StdLib"/> class.
        /// </summary>
        public StdLib StdLib { get => GetNativeContract<StdLib>(); }

        /// <summary>
        /// Gets the instance of the <see cref="Native.CryptoLib"/> class.
        /// </summary>
        public CryptoLib CryptoLib { get => GetNativeContract<CryptoLib>(); }

        /// <summary>
        /// Gets the instance of the <see cref="LedgerContract"/> class.
        /// </summary>
        public LedgerContract Ledger { get => GetNativeContract<LedgerContract>(); }

        /// <summary>
        /// Gets the instance of the <see cref="NeoToken"/> class.
        /// </summary>
        public NeoToken NEO { get => GetNativeContract<NeoToken>(); }

        /// <summary>
        /// Gets the instance of the <see cref="GasToken"/> class.
        /// </summary>
        public GasToken GAS { get => GetNativeContract<GasToken>(); }

        /// <summary>
        /// Gets the instance of the <see cref="PolicyContract"/> class.
        /// </summary>
        public virtual IPolicyContract Policy { get => GetNativeContract<IPolicyContract>(); }

        /// <summary>
        /// Gets the instance of the <see cref="Native.RoleManagement"/> class.
        /// </summary>
        public RoleManagement RoleManagement { get => GetNativeContract<RoleManagement>(); }

        /// <summary>
        /// Gets the instance of the <see cref="OracleContract"/> class.
        /// </summary>
        public OracleContract Oracle { get => GetNativeContract<OracleContract>(); }

        #endregion

        private T GetNativeContract<T>() where T : class
        {
            return s_contractsDictionary.Values.FirstOrDefault(x => x is T) as T ?? throw new NotSupportedException($"Native contract '{typeof(T).Name}' is not supported by the current repository.");
        }
    }
}
