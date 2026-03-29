using Ecommerce.Application.Common.Caching;
using Ecommerce.Domain.Common.Filters;
using Xunit;

namespace Ecommerce.UnitTests
{
    public class CacheKeyGeneratorTests
    {
        [Fact]
        public void User_ReturnsCorrectKey()
        {
            var key = CacheKeyGenerator.User(42);
            Assert.Equal("user:42", key);
        }

        [Fact]
        public void Category_ReturnsCorrectKey()
        {
            var key = CacheKeyGenerator.Category(5);
            Assert.Equal("category:5", key);
        }

        [Fact]
        public void HashFilter_WithNull_ReturnsEmptyString()
        {
            var hash = CacheKeyGenerator.HashFilter(null);
            Assert.Equal(string.Empty, hash);
        }

        [Fact]
        public void HashFilter_WithObject_ReturnsNonEmptyHash()
        {
            var filter = new ProductFilterDto { Keyword = "test" };
            var hash = CacheKeyGenerator.HashFilter(filter);
            Assert.False(string.IsNullOrEmpty(hash));
            Assert.DoesNotContain("/", hash);
            Assert.DoesNotContain("+", hash);
            Assert.DoesNotContain("=", hash);
        }

        [Fact]
        public void AuthSession_ReturnsCorrectKey()
        {
            var key = CacheKeyGenerator.AuthSession(100);
            Assert.Equal("auth:session:user:100", key);
        }

        [Fact]
        public void AuthSessionUserPrefix_ReturnsUserSessionKey()
        {
            var prefix = CacheKeyGenerator.AuthSessionUserPrefix(100);
            Assert.Equal("auth:session:user:100", prefix);
        }

        [Fact]
        public void BlacklistToken_ReturnsCorrectKey()
        {
            var key = CacheKeyGenerator.BlacklistToken("hash123");
            Assert.Equal("Blacklist:Token:hash123", key);
        }
    }
}
