using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoD2CharacterRoller
{


    enum RollType
    {
        SlotA = 1,
        SlotB = 2,
        SlotC = 4,
    }

    public class CharacterAttributes
    {
        public String Lang { get; private set; }

        public bool Using { get; set; }

        public SortableBindingList<CharacterAttribute> Attributes { get; private set; }

        public CharacterAttributes(
            String Lang, 
            bool Using
            )
        {
            this.Lang = Lang;
            this.Using = Using;
            this.Attributes = new SortableBindingList<CharacterAttribute>();
        }


    }
    public class CharacterAttribute
    {
        public String Name { get; set; }

        public int Weight { get; set; }

        public String Comment { get; set; }

        public CharacterAttribute(
            String Name,
            int Weight,
            String Comment
            )
        {
            this.Name = Name;
            this.Weight = Weight;
            this.Comment = Comment;
        }
    }

    public class CharacterRect
    {
        public int Left { get; set; }

        public int Top { get; set; }

        public int Right { get; set; }

        public int Bottom { get; set; }

        public int ButtonX { get; set; }

        public int ButtonY { get; set; }

        public CharacterRect(
            int left,
            int top,
            int right,
            int bottom,
            int buttonX,
            int buttonY
            )
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
            this.ButtonX = buttonX;
            this.ButtonY = buttonY;
        }

    }

    public class SortableBindingList<T> : BindingList<T>
    {
        // Fields
        private bool isSorted;
        private ListSortDirection listSortDirection;
        private PropertyDescriptor propertyDescriptor;

        // Methods
        public SortableBindingList()
        {
        }

        public SortableBindingList(IList<T> list)
            : this()
        {
            base.Items.Clear();
            foreach (T local in list)
            {
                base.Add(local);
            }
        }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            (base.Items as List<T>).Sort(this.GetComparisionDelegate(prop, direction));
        }

        private Comparison<T> GetComparisionDelegate(PropertyDescriptor propertyDescriptor, ListSortDirection direction)
        {
            return delegate (T t1, T t2)
            {
                int num2;
                ((SortableBindingList<T>)this).propertyDescriptor = propertyDescriptor;
                ((SortableBindingList<T>)this).listSortDirection = direction;
                ((SortableBindingList<T>)this).isSorted = true;
                int num = (direction == ListSortDirection.Ascending) ? 1 : -1;
                if (propertyDescriptor.PropertyType == typeof(string))
                {
                    num2 = StringComparer.CurrentCulture.Compare(propertyDescriptor.GetValue(t1), propertyDescriptor.GetValue(t2));
                }
                else
                {
                    num2 = Comparer<object>.Default.Compare(propertyDescriptor.GetValue(t1), propertyDescriptor.GetValue(t2));
                }
                return (num * num2);
            };
        }

        protected override void RemoveSortCore()
        {
            this.isSorted = false;
            this.propertyDescriptor = base.SortPropertyCore;
            this.listSortDirection = base.SortDirectionCore;
        }

        // Properties
        protected override bool IsSortedCore
        {
            get
            {
                return this.isSorted;
            }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get
            {
                return this.listSortDirection;
            }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get
            {
                return this.propertyDescriptor;
            }
        }

        protected override bool SupportsSortingCore
        {
            get
            {
                return true;
            }
        }
    }
}
