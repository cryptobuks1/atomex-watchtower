﻿using System;

namespace Atomex.Entities
{
    public class WalletAddress
    {
        public const int MaxNumberLength = 256;

        public string UniqueId => $"{Address}:{Currency}";
        public long Id { get; set; }
        public string Currency { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public decimal AllocatedBalance { get; set; }
        public decimal UnconfirmedIncome { get; set; }
        public decimal UnconfirmedOutcome { get; set; }
        public KeyIndex KeyIndex { get; set; }
        public bool HasActivity { get; set; }

        /// <summary>
        /// Public key in base64
        /// </summary>
        public string PublicKey { get; set; }
        /// <summary>
        /// Signature in base64
        /// </summary>
        public string ProofOfPossession { get; set; }
        public string Nonce { get; set; }

        public byte[] PublicKeyBytes()
        {
            return Convert.FromBase64String(PublicKey);
        }

        public decimal AvailableBalance(bool includeUnconfirmedIncome = false)
        {
            return includeUnconfirmedIncome
                ? Balance + UnconfirmedIncome + UnconfirmedOutcome
                : Balance + UnconfirmedOutcome;
        }

        public override string ToString()
        {
            return $"{Address};{Balance};{UnconfirmedIncome};{UnconfirmedOutcome}";
        }
    }
}