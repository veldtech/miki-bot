namespace Miki.Framework.Tests.Commands.Localization.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Miki.Cache;
    using Miki.Framework.Commands.Localization.Models;
    using Miki.Framework.Commands.Localization.Models.Exceptions;
    using Miki.Localization;
    using Miki.Localization.Models;
    using Miki.Services.Localization;
    using Moq;
    using Xunit;

    public class LocalizationServiceTests
    {
        ILocalizationService service;

        const long ValidId = 0L;
        const long InvalidId = 1L;

        public LocalizationServiceTests()
        {
            var dbContextMock = new Mock<DbContext>();
            dbContextMock
                .Setup(x => x.Set<ChannelLanguage>().FindAsync(It.Is<long>(x => x == ValidId)))
                .Returns<object[]>((id) => new ValueTask<ChannelLanguage>(
                    new ChannelLanguage { EntityId = (long)id[0], Language = "dut" }));
            dbContextMock
                .Setup(x => x.Set<ChannelLanguage>().FindAsync(It.Is<long>(x => x == InvalidId)))
                .Returns<object[]>((id) => new ValueTask<ChannelLanguage>(
                    new ChannelLanguage { EntityId = (long)id[0], Language = "swe" }));
               
            var cacheMock = new Mock<ICacheClient>();

            service = new LocalizationService(dbContextMock.Object, cacheMock.Object);
            service.AddLocale(new Locale("eng", null));
            service.AddLocale(new Locale("dut", null));
        }

        [Fact]
        public async Task GetValidLocaleTest()
        {
            Locale locale = await service.GetLocaleAsync(ValidId);

            Assert.NotNull(locale);
            Assert.Equal("dut", locale.CountryCode);
        }

        [Fact]
        public async Task GetDefaultLocaleTest()
        {
            Locale locale = await service.GetLocaleAsync(1510L);

            Assert.NotNull(locale);
            Assert.Equal("eng", locale.CountryCode);
        }

        [Fact]
        public Task GetInvalidLocaleTest()
        {
            return Assert.ThrowsAsync<LocaleNotFoundException>(
                async () => await service.GetLocaleAsync(InvalidId));
        }

        [Fact]
        public async Task ListLocalesTest()
        {
            var locales = new HashSet<string>();
            await foreach(var i in service.ListLocalesAsync())
            {
                locales.Add(i);
            }

            Assert.Contains("eng", locales);
            Assert.Contains("dut", locales);
            Assert.DoesNotContain("swe", locales);
        }

        [Fact]
        public async Task AddLocaleTest()
        {
            var locales = new HashSet<string>();
            await foreach(var i in service.ListLocalesAsync())
            {
                locales.Add(i);
            }
            Assert.Equal(2, locales.Count);
            
            service.AddLocale(new Locale("swe", null));

            var newLocales = new HashSet<string>();
            await foreach(var i in service.ListLocalesAsync())
            {
                newLocales.Add(i);
            }
            Assert.Equal(3, newLocales.Count);
        }
    }
}
