﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using QuranPhone.Resources;
using QuranPhone.Data;
using System.Windows.Controls;
using QuranPhone.Utils;
using QuranPhone.UI;

namespace QuranPhone.ViewModels
{
    public class DetailsViewModel : ViewModelBase
    {
        private const int PAGES_TO_PRELOAD = 2;

        public DetailsViewModel() : this(1)
        {       
        }

        public DetailsViewModel(int pageNumber)
        {
            this.Pages = new ObservableCollection<PageViewModel>();
            CurrentPageNumer = pageNumber;
        }

        public ObservableCollection<PageViewModel> Pages { get; private set; }
        private int[] loadedPages = new int[Constants.PAGES_LAST];

        private int currentPageNumber;
        public int CurrentPageNumer
        {
            get { return currentPageNumber; }
            set
            {
                if (value == currentPageNumber)
                    return;

                currentPageNumber = value;
                base.OnPropertyChanged(() => CurrentPageNumer);
            }
        }

        private int currentPageIndex;
        public int CurrentPageIndex
        {
            get { return currentPageIndex; }
            set
            {
                if (value == currentPageIndex)
                    return;

                currentPageIndex = value;
                updatePages();
                base.OnPropertyChanged(() => CurrentPageIndex);
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            if (Pages.Count == 0)
            {
                for (int i = CurrentPageNumer - PAGES_TO_PRELOAD; i <= CurrentPageNumer + PAGES_TO_PRELOAD; i++)
                {
                    var page = (i <= 0 ? Constants.PAGES_LAST + i : i);
                    Pages.Add(new PageViewModel(page));
                }
            }

            CurrentPageIndex = PAGES_TO_PRELOAD;
            this.IsDataLoaded = true;
        }

        #region Private Methods
        //Load only several pages
        private void updatePages()
        {
            if (Pages.Count == 0)
                return;

            var curPage = Pages[CurrentPageNumer].PageNumber;

            if (CurrentPageIndex == PAGES_TO_PRELOAD - 1)
            {
                var firstPage = Pages[0].PageNumber;
                var newPage = (firstPage - 1 <= 0 ? Constants.PAGES_LAST + firstPage - 1 : firstPage - 1);
                Pages.Insert(0, new PageViewModel(newPage));
                CurrentPageIndex++;                
            }
            else if (CurrentPageIndex == Pages.Count - PAGES_TO_PRELOAD)
            {
                var lastPage = Pages[Pages.Count - 1].PageNumber;
                var newPage = (lastPage + 1 >= Constants.PAGES_LAST ? Constants.PAGES_LAST - lastPage - 1 : lastPage + 1);
                Pages.Add(new PageViewModel(newPage));
            }
            Pages[CurrentPageIndex + PAGES_TO_PRELOAD].ImageSource = null;
            Pages[CurrentPageIndex - PAGES_TO_PRELOAD].ImageSource = null;
        }
             
        #endregion
    }
}