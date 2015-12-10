﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Languages.Editor.EditorHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Languages.Editor.Test.Text {
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TextChangesTest {
        [TestMethod]
        [TestCategory("Languages.Core")]
        public void TextChanges_DeleteInMiddle() {
            IList<TextChange> changes = BuildChangeList("abc", "adc");
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(new TextChange(1, 1, "d"), changes[0]);
        }

        [TestMethod]
        [TestCategory("Languages.Core")]
        public void TextChanges_DontBreakCRNL() {
            IList<TextChange> changes = BuildChangeList(" \n\n ", " \r\n ");
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(new TextChange(1, 2, "\r\n"), changes[0]);
        }

        [TestMethod]
        [TestCategory("Languages.Core")]
        public void TextChanges_DeleteOnlyAtStart() {
            IList<TextChange> changes = BuildChangeList("abc", "bc");
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(new TextChange(0, 1, ""), changes[0]);
        }

        private IList<TextChange> BuildChangeList(string oldText, string newText) {
            return TextChanges.BuildChangeList(oldText, newText, Int32.MaxValue);
        }
    }
}
