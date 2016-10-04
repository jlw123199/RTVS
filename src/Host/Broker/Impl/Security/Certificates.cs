﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Common.Core;

namespace Microsoft.R.Host.Broker.Security {
    internal static class Certificates {
        public static X509Certificate2 GetCertificateForEncryption(string certName) {
            return FindCertificate(certName);
        }

        private static X509Certificate2 FindCertificate(string name) {
            foreach (StoreName storeName in Enum.GetValues(typeof(StoreName))) {
                using (var store = new X509Store(storeName, StoreLocation.LocalMachine)) {
                    try {
                        store.Open(OpenFlags.OpenExistingOnly);
                    } catch(CryptographicException) {
                        continue;
                    }

                    try {
                        var collection = store.Certificates.Cast<X509Certificate2>();
                        var cert = collection.FirstOrDefault(c => c.FriendlyName.EqualsIgnoreCase(name));
                        if (cert == null) {
                            cert = collection.FirstOrDefault(c => c.Subject.IndexOfIgnoreCase(name) >= 0);
                            if (cert != null) {
                                return cert;
                            }
                        }
                    } finally {
                        store.Close();
                    }
                }
            }
            return null;
        }
    }
}
