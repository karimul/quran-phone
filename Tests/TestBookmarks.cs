﻿using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using QuranPhone.Data;
using QuranPhone.Utils;

namespace Tests
{
    [TestClass]
    public class TestBookmarks
    {
        [TestInitialize]
        public void TestCreateNewDatabase()
        {
            string basePath = QuranFileUtils.GetQuranDatabaseDirectory(false, true);
            if (basePath == null) return;
            string path = Path.Combine(basePath, BookmarksDBAdapter.DB_NAME);

            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            if (isf.FileExists(path))
            {
                isf.DeleteFile(path);
            }

            using (var bookmarks = new BookmarksDBAdapter())
            {
                Assert.IsTrue(isf.FileExists(path));
            }
        }

        [TestMethod]
        public void TestCreateBookmark()
        {
            using (var bookmarks = new BookmarksDBAdapter())
            {
                var initialCount = bookmarks.GetBookmarks(false, BoomarkSortOrder.Alphabetical).Count;
                bookmarks.AddBookmark(5);
                var newCount = bookmarks.GetBookmarks(false, BoomarkSortOrder.Alphabetical).Count;
                Assert.AreEqual(initialCount + 1, newCount);
            }
        }

        [TestMethod]
        public void TestCreateTags()
        {
            using (var bookmarks = new BookmarksDBAdapter())
            {
                var initialCount = bookmarks.GetTags().Count;
                bookmarks.AddTag("test");
                var newCount = bookmarks.GetTags().Count;
                Assert.AreEqual(initialCount + 1, newCount);
            }
        }
    }
}
