using Autofac.Extras.Moq;
using RcLibrary.Servcies.RatingCalculatorServices;
using System;
using Xunit;

namespace UnitTest;
public  class RcServiceTests
{
    // this data is obtained from raider.io of random runs during WWI S2
    [Theory]
    // level, time lime, run time, expected socre
    [InlineData(6, 2046000, 1485000, 240.2)] // Theater of Pain
    [InlineData(4, 1981000, 1312000, 212.7)] // Cinderbrew Meadery
    [InlineData(4, 1861000, 1865000, 184.9)] // Darkflame Cleft
    [InlineData(4, 1981000, 2074000, 183.2)] // Operation: Floodgate
    [InlineData(2, 1981000, 1493000, 164.2)] // The MOTHERLODE!!
    [InlineData(2, 1951000, 1599000, 161.8)] // Priory of the Sacred Flame
    [InlineData(2, 1921000, 1700000, 159.3)] // Mechagon Workshop
    [InlineData(2, 1861000, 1804000, 156.1)] // The Rookery
    [InlineData(17, 1861000, 1711000, 443)] // Darkflame Cleft
    [InlineData(17, 1861000, 1713000, 443)] // Darkflame Cleft
    [InlineData(17, 1981000, 1911000, 441.3)] // The MOTHERLODE!!
    [InlineData(17, 2046000, 1984000, 441)] // Theater of Pain
    [InlineData(17, 1921000, 2024000, 303)] // Mechagon Workshop
    [InlineData(17, 1861000, 2305000, 296)] // Darkflame Cleft
    [InlineData(16, 1861000, 1461000, 433)] // Darkflame Cleft
    [InlineData(16, 1861000, 1510000, 431.9)] // Darkflame Cleft
    [InlineData(16, 1861000, 1460000, 433.1)] // Darkflame Cleft
    [InlineData(16, 1861000, 1557000, 431.1)] // Darkflame Cleft
    [InlineData(16, 1861000, 1561000, 430.8)] // Darkflame Cleft
    [InlineData(16, 1861000, 1570000, 430.5)] // Darkflame Cleft
    [InlineData(16, 1861000, 1587000, 430.1)] // Darkflame Cleft
    [InlineData(16, 1861000, 1622000, 429.4)] // Darkflame Cleft
    [InlineData(16, 1861000, 1650000, 428.8)] // Darkflame Cleft
    [InlineData(16, 1861000, 1685000, 428.5)] // Darkflame Cleft
    [InlineData(11, 1981000, 2170000, 301.4)] // The MOTHERLODE!!
    [InlineData(11, 1921000, 2104000, 301.4)] // Mechagon Workshop
    [InlineData(11, 1951000, 2039000, 303.3)] // Priory of the Sacred Flame        
    [InlineData(11, 1951000, 2137000, 301.4)] // Priory of the Sacred Flame    
    [InlineData(11, 1861000, 2039000, 301.4)] // The Rookery
    public void GetDugneonScore_CalculatesCorrectScore(int level, double timeLimitMs, double clearTimeMs, double expectedScore)
    {
        using AutoMock mock = AutoMock.GetLoose();

        var service = mock.Create<RcService>();
        // Act
        var result = service.GetDugneonScore(clearTimeMs, timeLimitMs, level);

        // Assert
        Assert.True(Math.Abs(result - expectedScore) < 0.5, $"Expected ~{expectedScore}, got {result}");
    }
}
