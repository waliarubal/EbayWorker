using EbayWorker.Helpers.Base;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EbayWorker.Models
{
    public class BookCollection : NotificationBase, IList<BookModel>
    {
        List<BookModel> _books;
        int _brandNew, _likeNew, _veryGood, _good, _acceptable;

        public BookCollection()
        {
            _books = new List<BookModel>();
        }

        #region properties

        public IEnumerable<BookModel> Items
        {
            get { return _books; }
        }

        public int BrandNewCount
        {
            get { return _brandNew; }
            private set { Set(nameof(BrandNewCount), ref _brandNew, value); }
        }

        public int LikeNewCount
        {
            get { return _likeNew; }
            private set { Set(nameof(LikeNewCount), ref _likeNew, value); }
        }

        public int VeryGoodCount
        {
            get { return _veryGood; }
            private set { Set(nameof(VeryGoodCount), ref _veryGood, value); }
        }

        public int GoodCount
        {
            get { return _good; }
            private set { Set(nameof(GoodCount), ref _good, value); }
        }

        public int AcceptableCount
        {
            get { return _acceptable; }
            private set { Set(nameof(AcceptableCount), ref _acceptable, value); }
        }

        public BookModel this[int index]
        {
            get { return _books[index]; }
            set { _books[index] = value; }
        }

        public int Count
        {
            get { return _books.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("Condition"))
                return;

            var book = sender as BookModel;
            if (book == null)
                return;

            switch (book.Condition)
            {
                case BookCondition.BrandNew:
                    BrandNewCount++;
                    break;

                case BookCondition.LikeNew:
                    LikeNewCount++;
                    break;

                case BookCondition.VeryGood:
                    VeryGoodCount++;
                    break;

                case BookCondition.Good:
                    GoodCount++;
                    break;

                case BookCondition.Acceptable:
                    AcceptableCount++;
                    break;
            }
        }

        public void Add(BookModel item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            _books.Add(item);
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Count));
        }

        public void Clear()
        {
            _books.Clear();
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Count));
            BrandNewCount = LikeNewCount = VeryGoodCount = GoodCount = AcceptableCount = 0;
        }

        public bool Contains(BookModel item)
        {
            return _books.Contains(item);
        }

        public void CopyTo(BookModel[] array, int arrayIndex)
        {
            _books.CopyTo(array, arrayIndex);
        }

        public IEnumerator<BookModel> GetEnumerator()
        {
            return _books.GetEnumerator();
        }

        public int IndexOf(BookModel item)
        {
            return _books.IndexOf(item);
        }

        public void Insert(int index, BookModel item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            _books.Insert(index, item);
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Count));
        }

        public bool Remove(BookModel item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            switch (item.Condition)
            {
                case BookCondition.BrandNew:
                    if (BrandNewCount > 0)
                        BrandNewCount--;
                    break;

                case BookCondition.LikeNew:
                    if (LikeNewCount > 0)
                        LikeNewCount--;
                    break;

                case BookCondition.VeryGood:
                    if (VeryGoodCount > 0)
                        VeryGoodCount--;
                    break;

                case BookCondition.Good:
                    if (GoodCount > 0)
                        GoodCount--;
                    break;

                case BookCondition.Acceptable:
                    if (AcceptableCount > 0)
                        AcceptableCount--;
                    break;
            }
            var isRemoved = _books.Remove(item);
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Count));
            return isRemoved;
        }

        public void RemoveAt(int index)
        {
            var book = _books[index];
            Remove(book);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string ToCsvString(decimal addToPrice)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < Count; index++)
            {
                var book = this[index];
                builder.AppendLine(string.Format("{0},{1},{2:#####0.00}", book.Isbn, book.Condition, book.Price + addToPrice));
            }
            return builder.ToString();
        }

        public string ToCsvStringGroupedByCondition(decimal addToPrice)
        {
            if (Count == 0)
                return null;

            var firstBook = _books[0];

            var bn = new StringBuilder(string.Format("{0},{1},", firstBook.Isbn, BookCondition.BrandNew));
            var ln = new StringBuilder(string.Format("{0},{1},", firstBook.Isbn, BookCondition.LikeNew));
            var vg = new StringBuilder(string.Format("{0},{1},", firstBook.Isbn, BookCondition.VeryGood));
            var g = new StringBuilder(string.Format("{0},{1},", firstBook.Isbn, BookCondition.Good));
            var a = new StringBuilder(string.Format("{0},{1},", firstBook.Isbn, BookCondition.Acceptable));
            for(var index = 0; index < Count; index++)
            {
                var book = this[index];
                switch(book.Condition)
                {
                    case BookCondition.BrandNew:
                        bn.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.LikeNew:
                        ln.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.VeryGood:
                        vg.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.Good:
                        g.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.Acceptable:
                        a.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine(bn.ToString(0, bn.Length - 1));
            builder.AppendLine(ln.ToString(0, ln.Length - 1));
            builder.AppendLine(vg.ToString(0, vg.Length - 1));
            builder.AppendLine(g.ToString(0, g.Length - 1));
            builder.AppendLine(a.ToString(0, a.Length - 1));
            return builder.ToString();
        }

        public string ToCsvStringGroupedByConditionStupidLogic(decimal addToPrice)
        {
            if (Count == 0)
                return null;

            var firstBook = _books[0];

            var bn = new StringBuilder(string.Format("{0}BNC,,,", firstBook.Isbn));
            var ln = new StringBuilder(string.Format("{0}LNC,,,", firstBook.Isbn));
            var vg = new StringBuilder(string.Format("{0}VGC,,,", firstBook.Isbn));
            var g = new StringBuilder(string.Format("{0}GOC,,,", firstBook.Isbn));
            for (var index = 0; index < Count; index++)
            {
                var book = this[index];
                switch (book.Condition)
                {
                    case BookCondition.BrandNew:
                        bn.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        ln.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.LikeNew:
                        ln.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        vg.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.VeryGood:
                        vg.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        g.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;

                    case BookCondition.Good:
                        g.AppendFormat("{0:#####0.00},", book.Price + addToPrice);
                        break;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine(bn.ToString(0, bn.Length - 1));
            builder.AppendLine(ln.ToString(0, ln.Length - 1));
            builder.AppendLine(vg.ToString(0, vg.Length - 1));
            builder.AppendLine(g.ToString(0, g.Length - 1));
            return builder.ToString();
        }
    }
}
