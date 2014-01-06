﻿using System;
using System.Linq;
using FluentAssertions;
using Quran.Core.Common;
using Quran.Core.Data;
using Quran.Core.Utils;
using Xunit;
using Xunit.Extensions;

namespace Quran.Core.Tests.Utils
{
    public class AudioTests : BaseTest
    {
        [Fact]
        public void TestAudioRequestProperties()
        {
            var request = new AudioRequest(0, new QuranAyah(1, 2), new RepeatInfo(RepeatAmount.Juz, 2), 0, AudioDownloadAmount.Page);
            Assert.Equal("Minshawi Murattal (gapless)", request.Reciter.Name);
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
            Assert.Equal(RepeatAmount.Juz, request.RepeatInfo.RepeatAmount);
        }

        [Fact]
        public void AudioRequestThrowsArgumentNullException()
        {
            Assert.Throws(typeof(ArgumentNullException), () => new AudioRequest(null));
            Assert.Throws(typeof(ArgumentNullException), () => new AudioRequest(1, null, null, 0, AudioDownloadAmount.Page));
            Assert.Throws(typeof(ArgumentException), () => new AudioRequest("local://0/?amount=Page&fromAyah=a"));
            Assert.Throws(typeof(ArgumentException), () => new AudioRequest("aaa://0?amount=Page&fromAyah=1:2"));
            Assert.Throws(typeof(ArgumentException), () => new AudioRequest("local://aaa/?amount=Page&fromAyah=1:2"));
            Assert.Throws(typeof(ArgumentException), () => new AudioRequest(1, new QuranAyah(0, 0), null, 0, AudioDownloadAmount.Page));
        }

        [Fact]
        public void AudioRequestWorksWithStringConstructor()
        {
            var request = new AudioRequest("local://0/?amount=Surah&fromAyah=1:2");
            Assert.Equal("Minshawi Murattal (gapless)", request.Reciter.Name);
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
            Assert.Equal(AudioDownloadAmount.Surah, request.AudioDownloadAmount);
        }

        [Fact]
        public void AudioRequestToStringEqualsConstructor()
        {
            var pattern = "local://0/?amount=Surah&currentAyah=1:2&fromAyah=2:2&currentRepeat=0";
            var request = new AudioRequest(pattern);
            Assert.Equal(pattern, request.ToString());
        }

        [Fact]
        public void AudioRequestToStringEqualsConstructorWithAllParameters()
        {
            var pattern = "local://0/?amount=Juz&currentAyah=1:2&fromAyah=1:2&toAyah=2:2&repeat=Page-2-times&currentRepeat=2";
            var request = new AudioRequest(pattern);
            Assert.Equal(pattern, request.ToString());
        }

        [Theory]
        [InlineData(RepeatAmount.None, 0, "None-2-times")]
        [InlineData(RepeatAmount.None, 0, "blah")]
        [InlineData(RepeatAmount.None, 0, null)]
        [InlineData(RepeatAmount.None, 0, "")]
        [InlineData(RepeatAmount.Juz, 1, "Juz-1-times")]
        [InlineData(RepeatAmount.Page, 5, "Page-5-times")]
        [InlineData(RepeatAmount.OneAyah, int.MaxValue, "OneAyah-infinite-times")]
        public void RepeatParseFromString(RepeatAmount expAmount, int expCount, string pattern)
        {
            var repeat = RepeatInfo.FromString(pattern);
            Assert.Equal(expAmount, repeat.RepeatAmount);
            Assert.Equal(expCount, repeat.RepeatCount);
        }

        [Theory]
        [InlineData(RepeatAmount.None, 0, "None-0-times")]
        [InlineData(RepeatAmount.Juz, 1, "Juz-1-times")]
        [InlineData(RepeatAmount.Page, 5, "Page-5-times")]
        [InlineData(RepeatAmount.OneAyah, int.MaxValue, "OneAyah-infinite-times")]
        public void RepeatToStringMatch(RepeatAmount actAmount, int actCount, string pattern)
        {
            var repeat = new RepeatInfo {RepeatAmount = actAmount, RepeatCount = actCount};
            Assert.Equal(pattern, repeat.ToString());
        }

        [Fact]
        public void EmptyRepeatProducesNoneString()
        {
            var repeat = new RepeatInfo();
            Assert.Equal("None-0-times", repeat.ToString());
        }

        [Fact]
        public void AudioRequestGotoNextRepeatsAyah()
        {
            var request = new AudioRequest(0, new QuranAyah(1, 1), new RepeatInfo(RepeatAmount.OneAyah, 1), 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 1), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextRepeatsPage()
        {
            var request = new AudioRequest(0, new QuranAyah(2, 15), new RepeatInfo(RepeatAmount.Page, 1), 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(2, 16), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(2, 6), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextRepeatsThreeAyah()
        {
            var request = new AudioRequest(0, new QuranAyah(1, 1), new RepeatInfo(RepeatAmount.ThreeAyah, 1), 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 3), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 1), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextRepeatsJuz()
        {
            var request = new AudioRequest(0, new QuranAyah(2, 140), new RepeatInfo(RepeatAmount.Juz, 1), 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(2, 141), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 1), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
        }

        [Theory]
        [InlineData(1, 3, 1, 2)]
        [InlineData(114, 5, 114, 4)]
        [InlineData(1, 1, 114, 6)]
        [InlineData(2, 1, 2, 0)]
        public void AudioRequestGotoNextIncrementsAyah(int expSura, int expAya, int currSura, int currAya)
        {
            var request = new AudioRequest(0, new QuranAyah(currSura, currAya), null, 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(expSura, expAya), request.CurrentAyah);
        }

        [Theory]
        [InlineData(2, 0, 1, 7)]
        [InlineData(1, 1, 114, 6)]
        public void AudioRequestGotoNextReturnsBismillah(int expSura, int expAya, int currSura, int currAya)
        {
            var request = new AudioRequest(0, new QuranAyah(currSura, currAya), null, 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(expSura, expAya), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextDoesntReturnBismillahForTawba()
        {
            var request = new AudioRequest(0, new QuranAyah(8, 75), null, 0, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(9, 1), request.CurrentAyah);
        }

        [Theory]
        [InlineData(1, 1, 1, 2)]
        [InlineData(114, 6, 1, 1)]
        [InlineData(114, 3, 114, 4)]
        [InlineData(1, 7, 2, 0)]
        public void AudioRequestGotoPreviousDecrementsAyah(int expSura, int expAya, int currSura, int currAya)
        {
            var request = new AudioRequest(0, new QuranAyah(currSura, currAya), null, 0, AudioDownloadAmount.Page);
            request.GotoPreviousAyah();
            Assert.Equal(new QuranAyah(expSura, expAya), request.CurrentAyah);
        }

        [Theory]
        [InlineData(1, 1, 1, 2)]
        [InlineData(2, 0, 2, 1)]
        public void AudioRequestGotoPreviousReturnBismillah(int expSura, int expAya, int currSura, int currAya)
        {
            var request = new AudioRequest(0, new QuranAyah(currSura, currAya), null, 0, AudioDownloadAmount.Page);
            request.GotoPreviousAyah();
            Assert.Equal(new QuranAyah(expSura, expAya), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoPreviousDoesntReturnBismillahForTawba()
        {
            var request = new AudioRequest(0, new QuranAyah(9, 1), null, 0, AudioDownloadAmount.Page);
            request.GotoPreviousAyah();
            Assert.Equal(new QuranAyah(8, 75), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextRepeatsUpToMaxAmount()
        {
            var request = new AudioRequest(0, new QuranAyah(1, 1), new RepeatInfo(RepeatAmount.OneAyah, 2), 1, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 1), request.CurrentAyah);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
        }

        [Fact]
        public void AudioRequestGotoNextDoesntRepeat()
        {
            var request = new AudioRequest(0, new QuranAyah(1, 1), new RepeatInfo(RepeatAmount.OneAyah, 2), 2, AudioDownloadAmount.Page);
            request.GotoNextAyah();
            Assert.Equal(new QuranAyah(1, 2), request.CurrentAyah);
        }

        [Theory]
        [InlineData(true, 2, 1, 2, 2)]
        [InlineData(false, 2, 2, 2, 3)]
        [InlineData(false, 9, 1, 9, 5)]
        [InlineData(false, 8, 10, 9, 5)]
        [InlineData(true, 2, 200, 3, 5)]
        public void DoesRequireBismillahWorks(bool result, int startSura, int startAya, int endSura, int endAya)
        {
            var requires = AudioUtils.DoesRequireBismillah(new AudioRequest(5, new QuranAyah(startSura, startAya), null, 0, AudioDownloadAmount.Page) { ToAyah = new QuranAyah(endSura, endAya) });
            Assert.Equal(result, requires);
        }

        [Fact]
        public void DoesRequireBismillahWorksWithoutMaxAyah()
        {
            var requires = AudioUtils.DoesRequireBismillah(new AudioRequest(5, new QuranAyah(2, 1), null, 0, AudioDownloadAmount.Page));
            Assert.True(requires);
        }

        [Fact]
        public void GetFileNameForGaplessAyahGetsCorrectName()
        {
            var database = new RecitersDatabaseHandler();
            var reciter = database.GetGaplessReciters().First(r => r.LocalPath.Contains("Minshawi_Murattal_gapless"));
            var fileName = AudioUtils.GetLocalPathForAyah(new QuranAyah(2, 1), reciter);
            Assert.Equal("quran_android/audio/Minshawi_Murattal_gapless/002.mp3", fileName);
        }

        [Fact]
        public void GetFileNameForNonGaplessAyahGetsCorrectName()
        {
            var database = new RecitersDatabaseHandler();
            var reciter = database.GetNonGaplessReciters().First(r => r.LocalPath.Contains("Abd_Al-Basit"));
            var fileName = AudioUtils.GetLocalPathForAyah(new QuranAyah(2, 1), reciter);
            Assert.Equal("quran_android/audio/Abd_Al-Basit/002001.mp3", fileName);
        }

        [Theory]
        [InlineData(1, 1, 1, 7)]
        [InlineData(2, 6, 2, 16)]
        [InlineData(112, 1, 114, 6)]
        public void GetLastAyahToPlayWorksForPageFromPageBeginning(int startSura, int startAya, int endSura, int endAya)
        {
            var ayah = AudioUtils.GetLastAyahToPlay(new QuranAyah(startSura, startAya), AudioDownloadAmount.Page);
            Assert.Equal(new QuranAyah(endSura, endAya), ayah);
        }

        [Theory]
        [InlineData(1, 2, 1, 7)]
        [InlineData(2, 7, 2, 16)]
        [InlineData(112, 2, 114, 6)]
        public void GetLastAyahToPlayWorksForPageFromPageMiddle(int startSura, int startAya, int endSura, int endAya)
        {
            var ayah = AudioUtils.GetLastAyahToPlay(new QuranAyah(startSura, startAya), AudioDownloadAmount.Page);
            Assert.Equal(new QuranAyah(endSura, endAya), ayah);
        }

        [Theory]
        [InlineData(1, 7, 1, 7)]
        [InlineData(2, 16, 2, 16)]
        [InlineData(114, 6, 114, 6)]
        public void GetLastAyahToPlayWorksForPageFromPageEnd(int startSura, int startAya, int endSura, int endAya)
        {
            var ayah = AudioUtils.GetLastAyahToPlay(new QuranAyah(startSura, startAya), AudioDownloadAmount.Page);
            Assert.Equal(new QuranAyah(endSura, endAya), ayah);
        }

        [Theory]
        [InlineData(1, 1, 1, 7)]
        [InlineData(1, 2, 1, 7)]
        [InlineData(1, 7, 1, 7)]
        [InlineData(2, 1, 2, 286)]
        [InlineData(2, 5, 2, 286)]
        [InlineData(2, 286, 2, 286)]
        [InlineData(112, 1, 112, 4)]
        [InlineData(112, 2, 112, 4)]
        [InlineData(113, 1, 113, 5)]
        [InlineData(114, 6, 114, 6)]
        public void GetLastAyahToPlayWorksForSura(int startSura, int startAya, int endSura, int endAya)
        {
            var ayah = AudioUtils.GetLastAyahToPlay(new QuranAyah(startSura, startAya), AudioDownloadAmount.Surah);
            Assert.Equal(new QuranAyah(endSura, endAya), ayah);
        }

        [Theory]
        [InlineData(1, 1, 2, 141)]
        [InlineData(1, 22, 2, 141)]
        [InlineData(2, 141, 2, 141)]
        [InlineData(78, 1, 114, 6)]
        [InlineData(79, 22, 114, 6)]
        [InlineData(114, 1, 114, 6)]
        [InlineData(114, 6, 114, 6)]
        public void GetLastAyahToPlayWorksForJuz(int startSura, int startAya, int endSura, int endAya)
        {
            var ayah = AudioUtils.GetLastAyahToPlay(new QuranAyah(startSura, startAya), AudioDownloadAmount.Juz);
            Assert.Equal(new QuranAyah(endSura, endAya), ayah);
        }
    }
}
