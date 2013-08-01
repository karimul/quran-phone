﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using QuranPhone.Common;
using QuranPhone.Data;
using QuranPhone.Resources;
using QuranPhone.Utils;

namespace QuranPhone.ViewModels
{
    public class SearchViewModel : ViewModelBase
    {
        private const int MaxPreviewCharacter = 200;
        public SearchViewModel()
        {
            this.SearchResults = new ObservableCollection<ItemViewModel>();
        }

        #region Properties
        public ObservableCollection<ItemViewModel> SearchResults { get; private set; }

        private string query;
        public string Query
        {
            get { return query; }
            set
            {
                if (value == query)
                    return;

                query = value;

                base.OnPropertyChanged(() => Query);
            }
        }
        #endregion Properties

        #region Public methods

        public async void Load(string query)
        {
            // Set translation
            if ((string.IsNullOrEmpty(App.DetailsViewModel.TranslationFile) 
                || !QuranFileUtils.FileExists(Path.Combine(QuranFileUtils.GetQuranDatabaseDirectory(false), App.DetailsViewModel.TranslationFile))) 
                && !QuranFileUtils.FileExists(Path.Combine(QuranFileUtils.GetQuranDatabaseDirectory(false), QuranFileUtils.QURAN_ARABIC_DATABASE)))
            {
                MessageBox.Show(AppResources.no_translation_to_search);
            }
            else
            {
                IsLoading = true;
                try
                {
                    var translationVerses = new List<QuranAyah>();
                    var arabicVerses = new List<ArabicAyah>();
                    var tasks = new List<Task>();
                    var taskFactory = new TaskFactory();

                    if (App.DetailsViewModel.TranslationFile != null && 
                        QuranFileUtils.FileExists(Path.Combine(QuranFileUtils.GetQuranDatabaseDirectory(false), App.DetailsViewModel.TranslationFile)))
                    {
                        using (var db = new DatabaseHandler<QuranAyah>(App.DetailsViewModel.TranslationFile))
                        {
                            translationVerses = await taskFactory.StartNew(() => db.Search(query));
                        }
                    }
                    if (QuranFileUtils.FileExists(Path.Combine(QuranFileUtils.GetQuranDatabaseDirectory(false), QuranFileUtils.QURAN_ARABIC_DATABASE)))
                    {
                        using (var dbArabic = new DatabaseHandler<ArabicAyah>(QuranFileUtils.QURAN_ARABIC_DATABASE))
                        {
                            arabicVerses = await taskFactory.StartNew(() => dbArabic.Search(query));
                        }
                    }
                    this.SearchResults.Clear();

                    // Merging 2 results
                    int a = 0;
                    int t = 0;
                    var arabicVerse = new QuranAyah { Sura = int.MaxValue, Ayah = int.MaxValue };
                    var translationVerse = new QuranAyah { Sura = int.MaxValue, Ayah = int.MaxValue };
                    var verseToDisplay = new QuranAyah();
                    var comparer = new AyahComparer();

                    while (a < arabicVerses.Count || t < translationVerses.Count)
                    {
                        if (a < arabicVerses.Count)
                            arabicVerse = arabicVerses[a];
                        else
                            arabicVerse = new QuranAyah { Sura = int.MaxValue, Ayah = int.MaxValue };

                        if (t < translationVerses.Count)
                            translationVerse = translationVerses[t];
                        else
                            translationVerse = new QuranAyah { Sura = int.MaxValue, Ayah = int.MaxValue };

                        if (comparer.Compare(arabicVerse, translationVerse) > 0)
                        {
                            verseToDisplay = translationVerse;
                            t++;
                        }
                        else if (comparer.Compare(arabicVerse, translationVerse) < 0)
                        {
                            verseToDisplay = arabicVerse;
                            a++;
                        }
                        else if (comparer.Compare(arabicVerse, translationVerse) == 0)
                        {
                            verseToDisplay = arabicVerse;
                            a++;
                            t++;
                        }

                        var verse = verseToDisplay;
                        var text = TrimText(verse.Text, MaxPreviewCharacter);
                        this.SearchResults.Add(new ItemViewModel
                            {
                                Id =
                                    string.Format("{0} ({1}:{2})", QuranInfo.GetSuraName(verse.Sura, false), verse.Sura,
                                                  verse.Ayah),
                                Details = text,
                                PageNumber = QuranInfo.GetPageFromSuraAyah(verse.Sura, verse.Ayah),
                                SelectedAyah = new QuranAyah(verse.Sura, verse.Ayah)
                            });
                    }
                }
                catch (Exception e)
                {
                    this.SearchResults.Add(new ItemViewModel
                    {
                        Id = "Error",
                        Details = "Error performing translation",
                        PageNumber = 1,
                        SelectedAyah = new QuranAyah()
                    });
                }

                IsLoading = false;
            }
        }

        private string TrimText(string text, int maxPreviewCharacter)
        {
            if (text.Length <= MaxPreviewCharacter)
            {
                return text;
            }
            else
            {
                for (int i = MaxPreviewCharacter - 1; i >= 0; i--)
                {
                    if (text[i] == ' ')
                    {
                        return string.Format("{0}...", text.Substring(0, i));
                    }
                }
                return string.Format("{0}...", text.Substring(0, maxPreviewCharacter - 1));
            }
        }

        #endregion Public methods

        #region Private methods
        
        #endregion
    }
}