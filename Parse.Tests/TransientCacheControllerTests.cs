using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Parse.Infrastructure;

namespace Parse.Tests
{
    [TestClass]
    public class TransientCacheControllerTests
    {
        [TestMethod]
        [Description("Tests that the cache controller can add and then retrieve a value.")]
        public async Task LoadAsync_AddAsync_CanRetrieveValue()
        {
            // Arrange
            var cacheController = new TransientCacheController();
            var initialCache = await cacheController.LoadAsync();

            // Act
            await initialCache.AddAsync("testKey", "testValue");
            var finalCache = await cacheController.LoadAsync();
            finalCache.TryGetValue("testKey", out var result);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("testValue", result);
        }

        [TestMethod]
        [Description("Tests that RemoveAsync correctly removes an item from the cache.")]
        public async Task RemoveAsync_RemovesItemFromCache()
        {
            // Arrange
            var cacheController = new TransientCacheController();
            var cache = await cacheController.LoadAsync();
            await cache.AddAsync("testKey", "testValue");

            // Act
            await cache.RemoveAsync("testKey");
            var finalCache = await cacheController.LoadAsync();
            bool keyExists = finalCache.ContainsKey("testKey");

            // Assert
            Assert.IsFalse(keyExists);
        }

        [TestMethod]
        [Description("Tests that Clear correctly empties the cache.")]
        public async Task Clear_EmptiesTheCache()
        {
            // Arrange
            var cacheController = new TransientCacheController();
            var cache = await cacheController.LoadAsync();
            await cache.AddAsync("key1", "value1");
            await cache.AddAsync("key2", "value2");

            // Act
            cacheController.Clear();
            var finalCache = await cacheController.LoadAsync();

            // Assert
            Assert.AreEqual(0, finalCache.Count);
        }

        [TestMethod]
        [Description("Tests that GetRelativeFile throws NotSupportedException as expected.")]
        public void GetRelativeFile_ThrowsNotSupportedException()
        {
            var cacheController = new TransientCacheController();
            Assert.ThrowsException<NotSupportedException>(() => cacheController.GetRelativeFile("some/path"));
        }
    }
}
