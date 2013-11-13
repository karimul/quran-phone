﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Quran.Core.Common;
using Quran.Core.Data;
using Quran.Core.Properties;

namespace Quran.Core.Utils
{
    public class QuranInfo
    {
        public static string GetAyahTitle()
        {
            return AppResources.quran_ayah;
        }

        public static string GetSuraTitle()
        {
            return AppResources.quran_sura_title;
        }

        public static string GetJuzTitle()
        {
            return AppResources.quran_juz2;
        }

        public static string GetSuraName(int sura,
                                         bool wantTitle)
        {
            if (sura < Constants.SURA_FIRST || sura > Constants.SURA_LAST) { return ""; }
            string title = "";
            if (wantTitle) { title = GetSuraTitle() + " "; }

            using (StringReader stringReader = new StringReader(AppResources.sura_names))
            using (XmlReader reader = XmlReader.Create(stringReader)) {
                var doc = XDocument.Load(reader);
                return title + doc.Descendants("item").Skip(sura - 1).Take(1).Single().Value;
            }
        }

        public static string[] GetSuraQuarters()
        {
            List<string> results = new List<string>();
            using (StringReader stringReader = new StringReader(AppResources.quarters))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var doc = XDocument.Load(reader);
                foreach (var node in doc.Descendants("item"))
                {
                    results.Add(node.Value);
                }
            }
            return results.ToArray();
        }

        public static int GetSuraNumberFromPage(int page)
        {
            int sura = -1;
            for (int i = 0; i < Constants.SURAS_COUNT; i++)
            {
                if (SURA_PAGE_START[i] == page)
                {
                    sura = i + 1;
                    break;
                }
                else if (SURA_PAGE_START[i] > page)
                {
                    sura = i;
                    break;
                }
            }

            return sura;
        }

        public static int GetSuraNumberOfAyah(int surah)
        {
            if (surah >= Constants.SURA_FIRST && surah <= Constants.SURA_LAST)
            {
                return SURA_NUM_AYAHS[surah - 1];
            }
            else
            {
                return -1;
            }
        }

        public static string GetSuraNameFromPage(int page, bool wantTitle)
        {
            int sura = GetSuraNumberFromPage(page);
            return (sura > 0) ? GetSuraName(sura, wantTitle) : "";
        }

        public static string GetPageSubtitle(int page)
        {
            string description = AppResources.page_description;
            return string.Format(description, page, QuranInfo.GetJuzFromPage(page));
        }

        public static string GetJuzString(int page)
        {
            string description = AppResources.juz2_description;
            return string.Format(description, QuranInfo.GetJuzFromPage(page));
        }

        public static string GetSuraAyahString(int sura, int ayah)
        {
            string suraName = GetSuraName(sura, false);
            string format = AppResources.sura_ayah_notification_str;
            return string.Format(format, suraName, ayah);
        }

        public static ICollection<QuranAyah> GetAllAyah(QuranAyah fromAyah, QuranAyah toAyah)
        {
            var verses = new List<QuranAyah>();
            // Add first verse
            verses.Add(fromAyah);
            if (!Equals(fromAyah, toAyah))
            {
                var nextVerse = GetNextAyah(fromAyah);
                while (!Equals(nextVerse, toAyah))
                {
                    verses.Add(nextVerse);
                    nextVerse = GetNextAyah(nextVerse);
                }
                // Add last verse
                verses.Add(nextVerse);
            }
            return verses;
        }

        public static QuranAyah GetNextAyah(QuranAyah ayah)
        {
            var currentSurahPages = QuranInfo.GetSuraNumberOfAyah(ayah.Sura);
            var newAyah = new QuranAyah(ayah.Sura, ayah.Ayah);

            // Check if not the end of surah
            if (ayah.Ayah < currentSurahPages)
            {
                newAyah.Ayah++;
            }
            else
            {
                // If the end of surah check if also the end of Quran
                if (ayah.Sura < Constants.SURA_LAST)
                {
                    newAyah.Sura++;
                }
                else
                {
                    newAyah.Sura = Constants.SURA_FIRST;
                }
                newAyah.Ayah = 1;
            }
            return newAyah;
        }

        public static QuranAyah GetPreviousAyah(QuranAyah ayah)
        {
            var newAyah = new QuranAyah(ayah);

            // Check if not the beginning of surah
            if (ayah.Ayah > 1)
            {
                newAyah.Ayah--;
            }
            else
            {
                // If the beginning of surah check if also the beginning of Quran
                if (ayah.Sura > Constants.SURA_FIRST)
                {
                    newAyah.Sura--;
                }
                else
                {
                    newAyah.Sura = Constants.SURA_LAST;
                }
                newAyah.Ayah = QuranInfo.GetSuraNumberOfAyah(newAyah.Sura);
            }
            
            return newAyah;
        }

        public static string GetNotificationTitle(QuranAyah minVerse, QuranAyah maxVerse)
        {
            int minSura = minVerse.Sura;
            int maxSura = maxVerse.Sura;
            int maxAyah = maxVerse.Ayah;
            if (maxAyah == 0)
            {
                maxSura--;
                maxAyah = QuranInfo.GetSuraNumberOfAyah(maxSura);
            }

            string notificationTitle =
                    QuranInfo.GetSuraName(minSura, true);
            if (minSura == maxSura)
            {
                if (minVerse.Ayah == maxAyah)
                {
                    notificationTitle += " (" + maxAyah + ")";
                }
                else
                {
                    notificationTitle += " (" + minVerse.Ayah +
                         "-" + maxAyah + ")";
                }
            }
            else
            {
                notificationTitle += " (" + minVerse.Ayah +
                        ") - " + QuranInfo.GetSuraName(maxSura, true) +
                        " (" + maxAyah + ")";
            }

            return notificationTitle;
        }

        public static string GetSuraListMetaString(int sura)
        {
            string info = "";
            info += QuranInfo.SURA_IS_MAKKI[sura - 1] ? AppResources.makki : AppResources.madani;
            info += " - ";

            int ayahs = QuranInfo.SURA_NUM_AYAHS[sura - 1];
            info += ayahs.ToString(" 0 ", CultureInfo.InvariantCulture);
            if (ayahs == 1)
                info += AppResources.verse;
            else
                info += AppResources.verses;  
            return info;
        }

        #region Data

        public static int[] SURA_PAGE_START = {
		1, 2, 50, 77, 106, 128, 151, 177, 187, 208, 221, 235, 249, 255, 262,
		267, 282, 293, 305, 312, 322, 332, 342, 350, 359, 367, 377, 385, 396,
		404, 411, 415, 418, 428, 434, 440, 446, 453, 458, 467, 477, 483, 489,
		496, 499, 502, 507, 511, 515, 518, 520, 523, 526, 528, 531, 534, 537,
		542, 545, 549, 551, 553, 554, 556, 558, 560, 562, 564, 566, 568, 570,
		572, 574, 575, 577, 578, 580, 582, 583, 585, 586, 587, 587, 589, 590,
		591, 591, 592, 593, 594, 595, 595, 596, 596, 597, 597, 598, 598, 599,
		599, 600, 600, 601, 601, 601, 602, 602, 602, 603, 603, 603, 604, 604,
		604
	};

        public static int[] PAGE_SURA_START = {
		1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
		2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
		2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
		3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
		4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9,
		9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10,
		10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
		11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13,
		13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 16,
		16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 17,
		17, 17, 17, 17, 17, 17, 17, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18,
		19, 19, 19, 19, 19, 19, 19, 19, 20, 20, 20, 20, 20, 20, 20, 20, 20, 21,
		21, 21, 21, 21, 21, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 22, 22,
		22, 23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 24,
		24, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
		27, 27, 27, 27, 27, 27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 28,
		28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 31, 31,
		31, 31, 32, 32, 32, 33, 33, 33, 33, 33, 33, 33, 33, 33, 33, 34, 34, 34,
		34, 34, 34, 34, 35, 35, 35, 35, 35, 35, 36, 36, 36, 36, 36, 37, 37, 37,
		37, 37, 37, 37, 38, 38, 38, 38, 38, 38, 39, 39, 39, 39, 39, 39, 39, 39,
		39, 40, 40, 40, 40, 40, 40, 40, 40, 40, 41, 41, 41, 41, 41, 41, 42, 42,
		42, 42, 42, 42, 42, 43, 43, 43, 43, 43, 43, 44, 44, 44, 45, 45, 45, 45,
		46, 46, 46, 46, 47, 47, 47, 47, 48, 48, 48, 48, 48, 49, 49, 50, 50, 50,
		51, 51, 51, 52, 52, 53, 53, 53, 54, 54, 54, 55, 55, 55, 56, 56, 56, 57,
		57, 57, 57, 58, 58, 58, 58, 59, 59, 59, 60, 60, 60, 61, 62, 62, 63, 64,
		64, 65, 65, 66, 66, 67, 67, 67, 68, 68, 69, 69, 70, 70, 71, 72, 72, 73,
		73, 74, 74, 75, 76, 76, 77, 78, 78, 79, 80, 81, 82, 83, 83, 85, 86, 87,
		89, 89, 91, 92, 95, 97, 98, 100, 103, 106, 109, 112
	};

        public static int[] PAGE_AYAH_START = {
		1, 1, 6, 17, 25, 30, 38, 49, 58, 62, 70, 77, 84, 89, 94, 102, 106, 113,
		120, 127, 135, 142, 146, 154, 164, 170, 177, 182, 187, 191, 197, 203,
		211, 216, 220, 225, 231, 234, 238, 246, 249, 253, 257, 260, 265, 270,
		275, 282, 283, 1, 10, 16, 23, 30, 38, 46, 53, 62, 71, 78, 84, 92, 101,
		109, 116, 122, 133, 141, 149, 154, 158, 166, 174, 181, 187, 195, 1, 7,
		12, 15, 20, 24, 27, 34, 38, 45, 52, 60, 66, 75, 80, 87, 92, 95, 102, 
		106, 114, 122, 128, 135, 141, 148, 155, 163, 171, 176, 3, 6, 10, 14,
		18, 24, 32, 37, 42, 46, 51, 58, 65, 71, 77, 83, 90, 96, 104, 109, 114,
		1, 9, 19, 28, 36, 45, 53, 60, 69, 74, 82, 91, 95, 102, 111, 119, 125,
		132, 138, 143, 147, 152, 158, 1, 12, 23, 31, 38, 44, 52, 58, 68, 74,
		82, 88, 96, 105, 121, 131, 138, 144, 150, 156, 160, 164, 171, 179, 188,
		196, 1, 9, 17, 26, 34, 41, 46, 53, 62, 70, 1, 7, 14, 21, 27, 32, 37,
		41, 48, 55, 62, 69, 73, 80, 87, 94, 100, 107, 112, 118, 123, 1, 7, 15,
		21, 26, 34, 43, 54, 62, 71, 79, 89, 98, 107, 6, 13, 20, 29, 38, 46, 54,
		63, 72, 82, 89, 98, 109, 118, 5, 15, 23, 31, 38, 44, 53, 64, 70, 79,
		87, 96, 104, 1, 6, 14, 19, 29, 35, 43, 6, 11, 19, 25, 34, 43, 1, 16,
		32, 52, 71, 91, 7, 15, 27, 35, 43, 55, 65, 73, 80, 88, 94, 103, 111,
		119, 1, 8, 18, 28, 39, 50, 59, 67, 76, 87, 97, 105, 5, 16, 21, 28, 35,
		46, 54, 62, 75, 84, 98, 1, 12, 26, 39, 52, 65, 77, 96, 13, 38, 52, 65,
		77, 88, 99, 114, 126, 1, 11, 25, 36, 45, 58, 73, 82, 91, 102, 1, 6,
		16, 24, 31, 39, 47, 56, 65, 73, 1, 18, 28, 43, 60, 75, 90, 105, 1,
		11, 21, 28, 32, 37, 44, 54, 59, 62, 3, 12, 21, 33, 44, 56, 68, 1, 20,
		40, 61, 84, 112, 137, 160, 184, 207, 1, 14, 23, 36, 45, 56, 64, 77,
		89, 6, 14, 22, 29, 36, 44, 51, 60, 71, 78, 85, 7, 15, 24, 31, 39, 46,
		53, 64, 6, 16, 25, 33, 42, 51, 1, 12, 20, 29, 1, 12, 21, 1, 7, 16, 23,
		31, 36, 44, 51, 55, 63, 1, 8, 15, 23, 32, 40, 49, 4, 12, 19, 31, 39,
		45, 13, 28, 41, 55, 71, 1, 25, 52, 77, 103, 127, 154, 1, 17, 27, 43,
		62, 84, 6, 11, 22, 32, 41, 48, 57, 68, 75, 8, 17, 26, 34, 41, 50, 59,
		67, 78, 1, 12, 21, 30, 39, 47, 1, 11, 16, 23, 32, 45, 52, 11, 23, 34,
		48, 61, 74, 1, 19, 40, 1, 14, 23, 33, 6, 15, 21, 29, 1, 12, 20, 30, 1,
		10, 16, 24, 29, 5, 12, 1, 16, 36, 7, 31, 52, 15, 32, 1, 27, 45, 7, 28,
		50, 17, 41, 68, 17, 51, 77, 4, 12, 19, 25, 1, 7, 12, 22, 4, 10, 17, 1,
		6, 12, 6, 1, 9, 5, 1, 10, 1, 6, 1, 8, 1, 13, 27, 16, 43, 9, 35, 11, 40,
		11, 1, 14, 1, 20, 18, 48, 20, 6, 26, 20, 1, 31, 16, 1, 1, 1, 7, 35, 1,
		1, 16, 1, 24, 1, 15, 1, 1, 8, 10, 1, 1, 1, 1
	};

        public static int[] JUZ_PAGE_START = {
		1, 22, 42, 62, 82, 102, 121, 142, 162, 182,
		201, 222, 242, 262, 282, 302, 322, 342, 362, 382,
		402, 422, 442, 462, 482, 502, 522, 542, 562, 582
	};

        public static int[] RUB3_PAGE_START = {
		5, 7, 9, 11, 14, 17, 19, 22, 24,
		27, 29, 32, 34, 37, 39, 42, 44, 46, 49, 51, 54, 56, 59, 62, 64, 67,
		69, 72, 74, 77, 79, 82, 84, 87, 89, 92, 94, 97, 100, 102, 104, 106,
		109, 112, 114, 117, 119, 121, 124, 126, 129, 132, 134, 137, 140,
		142, 144, 146, 148, 151, 154, 156, 158, 162, 164, 167, 170, 173,
		175, 177, 179, 182, 184, 187, 189, 192, 194, 196, 199, 201, 204,
		206, 209, 212, 214, 217, 219, 222, 224, 226, 228, 231, 233, 236,
		238, 242, 244, 247, 249, 252, 254, 256, 259, 262, 264, 267, 270,
		272, 275, 277, 280, 282, 284, 287, 289, 292, 295, 297, 299, 302,
		304, 306, 309, 312, 315, 317, 319, 322, 324, 326, 329, 332, 334,
		336, 339, 342, 344, 347, 350, 352, 354, 356, 359, 362, 364, 367,
		369, 371, 374, 377, 379, 382, 384, 386, 389, 392, 394, 396, 399,
		402, 404, 407, 410, 413, 415, 418, 420, 422, 425, 426, 429, 431,
		433, 436, 439, 442, 444, 446, 449, 451, 454, 456, 459, 462, 464,
		467, 469, 472, 474, 477, 479, 482, 484, 486, 488, 491, 493, 496,
		499, 502, 505, 507, 510, 513, 515, 517, 519, 522, 524, 526, 529,
		531, 534, 536, 539, 542, 544, 547, 550, 553, 554, 558, 560, 562,
		564, 566, 569, 572, 575, 577, 579, 582, 585, 587, 589, 591, 594,
		596, 599
	};

        public static int[] PAGE_RUB3_START = {
		-1, -1, -1, -1, 1, -1, 2, -1, 3, -1, 4, -1, -1, 5, -1, -1, 6, -1, 7,
		-1, -1, 8, -1, 9, -1, -1, 10, -1, 11, -1, -1, 12, -1, 13, -1, -1, 14,
		-1, 15, -1, -1, 16, -1, 17, -1, 18, -1, -1, 19, -1, 20, -1, -1, 21, -1,
		22, -1, -1, 23, -1, -1, 24, -1, 25, -1, -1, 26, -1, 27, -1, -1, 28, -1,
		29, -1, -1, 30, -1, 31, -1, -1, 32, -1, 33, -1, -1, 34, -1, 35, -1, -1,
		36, -1, 37, -1, -1, 38, -1, -1, 39, -1, 40, -1, 41, -1, 42, -1, -1, 43,
		-1, -1, 44, -1, 45, -1, -1, 46, -1, 47, -1, 48, -1, -1, 49, -1, 50, -1,
		-1, 51, -1, -1, 52, -1, 53, -1, -1, 54, -1, -1, 55, -1, 56, -1, 57, -1,
		58, -1, 59, -1, -1, 60, -1, -1, 61, -1, 62, -1, 63, -1, -1, -1, 64, -1,
		65, -1, -1, 66, -1, -1, 67, -1, -1, 68, -1, 69, -1, 70, -1, 71, -1, -1,
		72, -1, 73, -1, -1, 74, -1, 75, -1, -1, 76, -1, 77, -1, 78, -1, -1, 79,
		-1, 80, -1, -1, 81, -1, 82, -1, -1, 83, -1, -1, 84, -1, 85, -1, -1, 86,
		-1, 87, -1, -1, 88, -1, 89, -1, 90, -1, 91, -1, -1, 92, -1, 93, -1, -1,
		94, -1, 95, -1, -1, -1, 96, -1, 97, -1, -1, 98, -1, 99, -1, -1, 100, -1,
		101, -1, 102, -1, -1, 103, -1, -1, 104, -1, 105, -1, -1, 106, -1, -1,
		107, -1, 108, -1, -1, 109, -1, 110, -1, -1, 111, -1, 112, -1, 113, -1,
		-1, 114, -1, 115, -1, -1, 116, -1, -1, 117, -1, 118, -1, 119, -1, -1,
		120, -1, 121, -1, 122, -1, -1, 123, -1, -1, 124, -1, -1, 125, -1, 126,
		-1, 127, -1, -1, 128, -1, 129, -1, 130, -1, -1, 131, -1, -1, 132, -1,
		133, -1, 134, -1, -1, 135, -1, -1, 136, -1, 137, -1, -1, 138, -1, -1,
		139, -1, 140, -1, 141, -1, 142, -1, -1, 143, -1, -1, 144, -1, 145, -1,
		-1, 146, -1, 147, -1, 148, -1, -1, 149, -1, -1, 150, -1, 151, -1, -1,
		152, -1, 153, -1, 154, -1, -1, 155, -1, -1, 156, -1, 157, -1, 158, -1,
		-1, 159, -1, -1, 160, -1, 161, -1, -1, 162, -1, -1, 163, -1, -1, 164,
		-1, 165, -1, -1, 166, -1, 167, -1, 168, -1, -1, 169, 170, -1, -1, 171,
		-1, 172, -1, 173, -1, -1, 174, -1, -1, 175, -1, -1, 176, -1, 177, -1,
		178, -1, -1, 179, -1, 180, -1, -1, 181, -1, 182, -1, -1, 183, -1, -1,
		184, -1, 185, -1, -1, 186, -1, 187, -1, -1, 188, -1, 189, -1, -1, 190,
		-1, 191, -1, -1, 192, -1, 193, -1, 194, -1, 195, -1, -1, 196, -1, 197,
		-1, -1, 198, -1, -1, 199, -1, -1, 200, -1, -1, 201, -1, 202, -1, -1,
		203, -1, -1, 204, -1, 205, -1, 206, -1, 207, -1, -1, 208, -1, 209, -1,
		210, -1, -1, 211, -1, 212, -1, -1, 213, -1, 214, -1, -1, 215, -1, -1,
		216, -1, 217, -1, -1, 218, -1, -1, 219, -1, -1, 220, 221, -1, -1, -1,
		222, -1, 223, -1, 224, -1, 225, -1, 226, -1, -1, 227, -1, -1, 228, -1,
		-1, 229, -1, 230, -1, 231, -1, -1, 232, -1, -1, 233, -1, 234, -1, 235,
		-1, 236, -1, -1, 237, -1, 238, -1, -1, 239, -1, -1, -1, -1, -1
	};

        public static int[] SURA_NUM_AYAHS = {
		7, 286, 200, 176, 120, 165, 206, 75, 129, 109, 123, 111,
		43, 52, 99, 128, 111, 110, 98, 135, 112, 78, 118, 64, 77,
		227, 93, 88, 69, 60, 34, 30, 73, 54, 45, 83, 182, 88, 75,
		85, 54, 53, 89, 59, 37, 35, 38, 29, 18, 45, 60, 49, 62, 55,
		78, 96, 29, 22, 24, 13, 14, 11, 11, 18, 12, 12, 30, 52, 52,
		44, 28, 28, 20, 56, 40, 31, 50, 40, 46, 42, 29, 19, 36, 25,
		22, 17, 19, 26, 30, 20, 15, 21, 11, 8, 8, 19, 5, 8, 8, 11,
		11, 8, 3, 9, 5, 4, 7, 3, 6, 3, 5, 4, 5, 6
	};

        public static bool[] SURA_IS_MAKKI = {
      // 1 - 10
		true, false, false, false, false, true, true, false, false, true,
      // 11 - 20
		true, true, false, true, true, true, true, true, true, true,
      // 21 - 30
		true, false, true, false, true, true, true, true, true, true,
      // 31 - 40
		true, true, false, true, true, true, true, true, true, true,
      // 41 - 50
		true, true, true, true, true, true, false, false, false, true,
      // 51 - 60
		true, true, true, true, false, true, false, false, false, false,
      // 61 - 70
		false, false, false, false, false, false, true, true, true, true,
      // 71 - 80
		true, true, true, true, true, false, true, true, true, true,
      // 81 - 90
		true, true, true, true, true, true, true, true, true, true,
      // 91 - 100
		true, true, true, true, true, true, true, true, false, true,
      // 101 - 110
		true, true, true, true, true, true, true, true, true, false,
      // 111 - 114
		true, true, true, true
	};

        public static int[][] QUARTERS = new int[][]{
	      // hizb 1
	      new int[]{1, 1}, new int[] {2, 26}, new int[] {2, 44}, new int[] {2, 60}, 

	      // hizb 2
	      new int[] {2, 75}, new int[] {2, 92}, new int[] {2, 106}, new int[] {2, 124}, 

	      // hizb 3
	      new int[] {2, 142}, new int[] {2, 158}, new int[] {2, 177}, new int[] {2, 189}, 

	      // hizb 4
	      new int[] {2, 203}, new int[] {2, 219}, new int[] {2, 233}, new int[] {2, 243}, 

	      // hizb 5
	      new int[] {2, 253}, new int[] {2, 263}, new int[] {2, 272}, new int[] {2, 283}, 

	      // hizb 6
	      new int[] {3, 15}, new int[] {3, 33}, new int[] {3, 52}, new int[] {3, 75}, 

	      // hizb 7
	      new int[] {3, 93}, new int[] {3, 113}, new int[] {3, 133}, new int[] {3, 153}, 

	      // hizb 8
	      new int[] {3, 171}, new int[] {3, 186}, new int[] {4, 1}, new int[] {4, 12}, 

	      // hizb 9
	      new int[] {4, 24}, new int[] {4, 36}, new int[] {4, 58}, new int[] {4, 74}, 

	      // hizb 10
	      new int[] {4, 88}, new int[] {4, 100}, new int[] {4, 114}, new int[] {4, 135}, 

	      // hizb 11
	      new int[] {4, 148}, new int[] {4, 163}, new int[] {5, 1}, new int[] {5, 12}, 

	      // hizb 12
	      new int[] {5, 27}, new int[] {5, 41}, new int[] {5, 51}, new int[] {5, 67}, 

	      // hizb 13
	      new int[] {5, 82}, new int[] {5, 97}, new int[] {5, 109}, new int[] {6, 13}, 

	      // hizb 14
	      new int[] {6, 36}, new int[] {6, 59}, new int[] {6, 74}, new int[] {6, 95}, 

	      // hizb 15
	      new int[] {6, 111}, new int[] {6, 127}, new int[] {6, 141}, new int[] {6, 151}, 

	      // hizb 16
	      new int[] {7, 1}, new int[] {7, 31}, new int[] {7, 47}, new int[] {7, 65}, 

	      // hizb 17
	      new int[] {7, 88}, new int[] {7, 117}, new int[] {7, 142}, new int[] {7, 156}, 

	      // hizb 18
	      new int[] {7, 171}, new int[] {7, 189}, new int[] {8, 1}, new int[] {8, 22}, 

	      // hizb 19
	      new int[] {8, 41}, new int[] {8, 61}, new int[] {9, 1}, new int[] {9, 19}, 

	      // hizb 20
	      new int[] {9, 34}, new int[] {9, 46}, new int[] {9, 60}, new int[] {9, 75}, 

	      // hizb 21
	      new int[] {9, 93}, new int[] {9, 111}, new int[] {9, 122}, new int[] {10, 11}, 

	      // hizb 22
	      new int[] {10, 26}, new int[] {10, 53}, new int[] {10, 71}, new int[] {10, 90}, 

	      // hizb 23
	      new int[] {11, 6}, new int[] {11, 24}, new int[] {11, 41}, new int[] {11, 61}, 

	      // hizb 24
	      new int[] {11, 84}, new int[] {11, 108}, new int[] {12, 7}, new int[] {12, 30}, 

	      // hizb 25
	      new int[] {12, 53}, new int[] {12, 77}, new int[] {12, 101}, new int[] {13, 5}, 

	      // hizb 26
	      new int[] {13, 19}, new int[] {13, 35}, new int[] {14, 10}, new int[] {14, 28}, 

	      // hizb 27
	      new int[] {15, 1}, new int[] {15, 50}, new int[] {16, 1}, new int[] {16, 30}, 

	      // hizb 28
	      new int[] {16, 51}, new int[] {16, 75}, new int[] {16, 90}, new int[] {16, 111}, 

	      // hizb 29
	      new int[] {17, 1}, new int[] {17, 23}, new int[] {17, 50}, new int[] {17, 70}, 

	      // hizb 30
	      new int[] {17, 99}, new int[] {18, 17}, new int[] {18, 32}, new int[] {18, 51}, 

	      // hizb 31
	      new int[] {18, 75}, new int[] {18, 99}, new int[] {19, 22}, new int[] {19, 59}, 

	      // hizb 32
	      new int[] {20, 1}, new int[] {20, 55}, new int[] {20, 83}, new int[] {20, 111}, 

	      // hizb 33
	      new int[] {21, 1}, new int[] {21, 29}, new int[] {21, 51}, new int[] {21, 83}, 

	      // hizb 34
	      new int[] {22, 1}, new int[] {22, 19}, new int[] {22, 38}, new int[] {22, 60}, 

	      // hizb 35
	      new int[] {23, 1}, new int[] {23, 36}, new int[] {23, 75}, new int[] {24, 1}, 

	      // hizb 36
	      new int[] {24, 21}, new int[] {24, 35}, new int[] {24, 53}, new int[] {25, 1}, 

	      // hizb 37
	      new int[] {25, 21}, new int[] {25, 53}, new int[] {26, 1}, new int[] {26, 52}, 

	      // hizb 38
	      new int[] {26, 111}, new int[] {26, 181}, new int[] {27, 1}, new int[] {27, 27}, 

	      // hizb 39
	      new int[] {27, 56}, new int[] {27, 82}, new int[] {28, 12}, new int[] {28, 29}, 

	      // hizb 40
	      new int[] {28, 51}, new int[] {28, 76}, new int[] {29, 1}, new int[] {29, 26}, 

	      // hizb 41
	      new int[] {29, 46}, new int[] {30, 1}, new int[] {30, 31}, new int[] {30, 54}, 

	      // hizb 42
	      new int[] {31, 22}, new int[] {32, 11}, new int[] {33, 1}, new int[] {33, 18}, 

	      // hizb 43
	      new int[] {33, 31}, new int[] {33, 51}, new int[] {33, 60}, new int[] {34, 10}, 

	      // hizb 44
	      new int[] {34, 24}, new int[] {34, 46}, new int[] {35, 15}, new int[] {35, 41}, 

	      // hizb 45
	      new int[] {36, 28}, new int[] {36, 60}, new int[] {37, 22}, new int[] {37, 83}, 

	      // hizb 46
	      new int[] {37, 145}, new int[] {38, 21}, new int[] {38, 52}, new int[] {39, 8}, 

	      // hizb 47
	      new int[] {39, 32}, new int[] {39, 53}, new int[] {40, 1}, new int[] {40, 21}, 

	      // hizb 48
	      new int[] {40, 41}, new int[] {40, 66}, new int[] {41, 9}, new int[] {41, 25}, 

	      // hizb 49
	      new int[] {41, 47}, new int[] {42, 13}, new int[] {42, 27}, new int[] {42, 51}, 

	      // hizb 50
	      new int[] {43, 24}, new int[] {43, 57}, new int[] {44, 17}, new int[] {45, 12}, 

	      // hizb 51
	      new int[] {46, 1}, new int[] {46, 21}, new int[] {47, 10}, new int[] {47, 33}, 

	      // hizb 52
	      new int[] {48, 18}, new int[] {49, 1}, new int[] {49, 14}, new int[] {50, 27}, 

	      // hizb 53
	      new int[] {51, 31}, new int[] {52, 24}, new int[] {53, 26}, new int[] {54, 9}, 

	      // hizb 54
	      new int[] {55, 1}, new int[] {56, 1}, new int[] {56, 75}, new int[] {57, 16}, 

	      // hizb 55
	      new int[] {58, 1}, new int[] {58, 14}, new int[] {59, 11}, new int[] {60, 7}, 

	      // hizb 56
	      new int[] {62, 1}, new int[] {63, 4}, new int[] {65, 1}, new int[] {66, 1}, 

	      // hizb 57
	      new int[] {67, 1}, new int[] {68, 1}, new int[] {69, 1}, new int[] {70, 19}, 

	      // hizb 58
	      new int[] {72, 1}, new int[] {73, 20}, new int[] {75, 1}, new int[] {76, 19}, 

	      // hizb 59
	      new int[] {78, 1}, new int[] {80, 1}, new int[] {82, 1}, new int[] {84, 1}, 

	      // hizb 60
	      new int[] {87, 1}, new int[] {90, 1}, new int[] {94, 1}, new int[] {100, 9}, 
	    };

        #endregion

        public static int[] GetPageBounds(int page)
        {
            if (page > Constants.PAGES_LAST)
                page = Constants.PAGES_LAST;
            if (page < 1) page = 1;

            int[] bounds = new int[4];
            bounds[0] = PAGE_SURA_START[page - 1];
            bounds[1] = PAGE_AYAH_START[page - 1];
            if (page == Constants.PAGES_LAST)
            {
                bounds[2] = Constants.SURA_LAST;
                bounds[3] = 6;
            }
            else
            {
                int nextPageSura = PAGE_SURA_START[page];
                int nextPageAyah = PAGE_AYAH_START[page];

                if (nextPageSura == bounds[0])
                {
                    bounds[2] = bounds[0];
                    bounds[3] = nextPageAyah - 1;
                }
                else
                {
                    if (nextPageAyah > 1)
                    {
                        bounds[2] = nextPageSura;
                        bounds[3] = nextPageAyah - 1;
                    }
                    else
                    {
                        bounds[2] = nextPageSura - 1;
                        bounds[3] = SURA_NUM_AYAHS[bounds[2] - 1];
                    }
                }
            }
            return bounds;
        }

        public static string GetSuraNameFromPage(int page)
        {
            for (int i = 0; i < Constants.SURAS_COUNT; i++)
            {
                if (SURA_PAGE_START[i] == page)
                {
                    return GetSuraName(i + 1, false);
                }
                else if (SURA_PAGE_START[i] > page)
                {
                    return GetSuraName(i, false);
                }
            }
            return "";
        }

        public static int GetJuzFromPage(int page)
        {
            int juz = ((page - 2) / 20) + 1;
            return juz > 30 ? 30 : juz < 1 ? 1 : juz;
        }

        public static int GetRub3FromPage(int page)
        {
            if ((page > Constants.PAGES_LAST) || (page < 1)) return -1;
            for (int i = page - 1; i >= 0; i--)
            {
                if (PAGE_RUB3_START[i] != -1)
                    return PAGE_RUB3_START[i];
            }
            return 0;
        }

        public static int GetPageFromSuraAyah(QuranAyah ayah)
        {
            return GetPageFromSuraAyah(ayah.Sura, ayah.Ayah);
        }

        public static int GetPageFromSuraAyah(int sura, int ayah)
        {
            // basic bounds checking
            if (ayah == 0) ayah = 1;
            if ((sura < 1) || (sura > Constants.SURAS_COUNT)
                    || (ayah < Constants.AYA_MIN) ||
                   (ayah > Constants.AYA_MAX))
                return -1;

            // what page does the sura start on?
            int index = QuranInfo.SURA_PAGE_START[sura - 1] - 1;
            while (index < Constants.PAGES_LAST)
            {
                // what's the first sura in that page?
                int ss = QuranInfo.PAGE_SURA_START[index];

                // if we've passed the sura, return the previous page
                // or, if we're at the same sura and passed the ayah
                if (ss > sura || ((ss == sura) &&
                     (QuranInfo.PAGE_AYAH_START[index] > ayah)))
                {
                    break;
                }

                // otherwise, look at the next page
                index++;
            }

            return index;
        }

        public static int GetAyahId(int sura, int ayah)
        {
            int ayahId = 0;
            for (int i = 0; i < sura - 1; i++)
            {
                ayahId += SURA_NUM_AYAHS[i];
            }
            ayahId += ayah;
            return ayahId;
        }

        public static int GetAyahId(int page)
        {
            int sura = PAGE_SURA_START[page - 1];
            int ayah = PAGE_AYAH_START[page - 1];
            return GetAyahId(sura, ayah);
        }

        public static int[] GetSuraAyahFromAyahId(int ayahId)
        {
            int lastCount = 0;
            int ayahCount = 0;
            int sura = 0;
            while (ayahCount < ayahId)
            {
                lastCount = ayahCount;
                ayahCount += SURA_NUM_AYAHS[sura];
                sura++;
            }
            int ayah = ayahId - lastCount;
            int[] values = { sura, ayah };
            return values;
        }

        public static string GetAyahString(int sura, int ayah)
        {
            return GetSuraName(sura, true) + " - "
                    + GetAyahTitle() + " " + ayah;
        }

        public static string GetSuraNameString(int page)
        {
            return GetSuraTitle() + " " + GetSuraNameFromPage(page);
        }

        public static bool DoesStringContainArabic(string s)
        {
            if (s == null) return false;

            int length = s.Length;
            for (int i = 0; i < length; i++)
            {
                int current = (int)s[i];
                // Skip space
                if (current == 32)
                    continue;
                // non-reshaped arabic
                if ((current >= 1570) && (current <= 1610))
                    return true;
                // re-shaped arabic
                else if ((current >= 65133) && (current <= 65276))
                    return true;
                // if the value is 42, it deserves another chance :p
                // (in reality, 42 is a * which is useful in searching sqlite)
                else if (current != 42)
                    return false;
            }
            return false;
        }
    }
}
