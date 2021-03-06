using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dracoon.Crypto.Sdk.Model;
using System;
using System.IO;

namespace Dracoon.Crypto.Sdk.Test {
    [TestClass()]
    public class FileDecryptionCipherTests {

        #region Single block decryption tests

        [TestMethod()]
        public void TestDecryptSingleBlock_Success() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key);
            byte[] fileTag = Convert.FromBase64String(pfk.Tag);
            byte[] ef = Convert.FromBase64String(TestResources.enc_file);
            byte[] pf = Convert.FromBase64String(TestResources.plain_file);

            EncryptedDataContainer testEdc = new EncryptedDataContainer(ef, fileTag);
            PlainDataContainer testPdc = TestDecryptSingleBlock(pfk, testEdc);
            System.Diagnostics.Debug.WriteLine(Convert.ToBase64String(testPdc.Content));
            System.Diagnostics.Debug.WriteLine(Convert.ToBase64String(pf));
            CollectionAssert.AreEqual(pf, testPdc.Content, "File content does not match!");
        }
        [TestMethod()]
        public void TestDecryptSingleBlock_ModifiedContent() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key);
            byte[] ft = Convert.FromBase64String(pfk.Tag);
            byte[] efc = Convert.FromBase64String(TestResources.enc_file_modified);
            EncryptedDataContainer testEdc = new EncryptedDataContainer(efc, ft);
            try {
                TestDecryptSingleBlock(pfk, testEdc);
            } catch (BadFileException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptSingleBlock_ModifiedTag() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key_bad_tag);
            byte[] ft = Convert.FromBase64String(pfk.Tag);
            byte[] efc = Convert.FromBase64String(TestResources.enc_file);

            EncryptedDataContainer testEdc = new EncryptedDataContainer(efc, ft);
            try {
                TestDecryptSingleBlock(pfk, testEdc);
            } catch (BadFileException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptSingleBlock_ModifiedKey() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key_bad_key);
            byte[] ft = Convert.FromBase64String(pfk.Tag);
            byte[] efc = Convert.FromBase64String(TestResources.enc_file);

            EncryptedDataContainer testEdc = new EncryptedDataContainer(efc, ft);
            try {
                TestDecryptSingleBlock(pfk, testEdc);
            } catch (BadFileException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptSingleBlock_ModifiedIv() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key_bad_iv);
            byte[] ft = Convert.FromBase64String(pfk.Tag);
            byte[] efc = Convert.FromBase64String(TestResources.enc_file);

            EncryptedDataContainer testEdc = new EncryptedDataContainer(efc, ft);
            try {
                TestDecryptSingleBlock(pfk, testEdc);
            } catch (BadFileException) {
                return;
            }
            Assert.Fail();
        }

        private PlainDataContainer TestDecryptSingleBlock(PlainFileKey pfk, EncryptedDataContainer edc) {
            FileDecryptionCipher decryptCipher = Crypto.CreateFileDecryptionCipher(pfk);

            using (MemoryStream ms = new MemoryStream()) {
                PlainDataContainer pdc = decryptCipher.ProcessBytes(new EncryptedDataContainer(edc.Content, null));
                ms.Write(pdc.Content, 0, pdc.Content.Length);
                pdc = decryptCipher.DoFinal(new EncryptedDataContainer(null, edc.Tag));
                ms.Write(pdc.Content, 0, pdc.Content.Length);
                return new PlainDataContainer(ms.ToArray());
            }
        }
        #endregion

        #region Multi block decryption tests

        [TestMethod()]
        public void TestDecryptMultiBlock_Success() {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key);
            byte[] ft = Convert.FromBase64String(pfk.Tag);
            byte[] efc = Convert.FromBase64String(TestResources.enc_file);
            byte[] pfc = Convert.FromBase64String(TestResources.plain_file);

            FileDecryptionCipher decryptCipher = Crypto.CreateFileDecryptionCipher(pfk);

            using (MemoryStream output = new MemoryStream()) {
                using (MemoryStream input = new MemoryStream(efc)) {
                    byte[] buffer = new byte[16];
                    int bytesRead;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) != 0) {
                        byte[] blockBytes = new byte[bytesRead];
                        Array.Copy(buffer, blockBytes, bytesRead);
                        PlainDataContainer currentPdc = decryptCipher.ProcessBytes(new EncryptedDataContainer(blockBytes, null));
                        output.Write(currentPdc.Content, 0, currentPdc.Content.Length);
                    }
                }
                PlainDataContainer testPdc = decryptCipher.DoFinal(new EncryptedDataContainer(null, ft));
                output.Write(testPdc.Content, 0, testPdc.Content.Length);
                byte[] testPfc = output.ToArray();
                CollectionAssert.AreEqual(pfc, testPfc, "File content does not match!");
            }
        }
        #endregion

        #region Illegal data container tests

        #region ProcessBytes

        [TestMethod()]
        public void TestDecryptProcessArguments_InvalidDataContainer() {
            try {
                TestDecryptProcessArguments(null);
            } catch (ArgumentNullException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptProcessArguments_InvalidDataContent() {
            try {
                TestDecryptProcessArguments(new EncryptedDataContainer(null, null));
            } catch (ArgumentNullException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptProcessArguments_InvalidDataTag() {
            try {
                TestDecryptProcessArguments(new EncryptedDataContainer(new byte[] { }, new byte[] { }));
            } catch (ArgumentException) {
                return;
            }
            Assert.Fail();
        }
        private void TestDecryptProcessArguments(EncryptedDataContainer edc) {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key);
            FileDecryptionCipher decCipher = Crypto.CreateFileDecryptionCipher(pfk);
            decCipher.ProcessBytes(edc);
        }
        #endregion

        #region DoFinal
        [TestMethod()]
        public void TestDecryptDoFinalArguments_InvalidDataContainer() {
            try {
                TestDecryptDoFinalArguments(null);
            } catch (ArgumentNullException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptDoFinalArguments_InvalidDataContent() {
            try {
                TestDecryptDoFinalArguments(new EncryptedDataContainer(null, null));
            } catch (ArgumentNullException) {
                return;
            }
            Assert.Fail();
        }
        [TestMethod()]
        public void TestDecryptDoFinalArguments_InvalidDataTag() {
            try {
                TestDecryptDoFinalArguments(new EncryptedDataContainer(new byte[] { }, new byte[] { }));
            } catch (ArgumentException) {
                return;
            }
            Assert.Fail();
        }
        private void TestDecryptDoFinalArguments(EncryptedDataContainer edc) {
            PlainFileKey pfk = TestUtilities.ReadTestResource<PlainFileKey>(TestResources.plain_file_key);
            FileDecryptionCipher decCipher = Crypto.CreateFileDecryptionCipher(pfk);
            decCipher.DoFinal(edc);
        }
        #endregion

        #endregion
    }
}