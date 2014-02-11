using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace NanoTrans
{
    public static class UIHelper
    {
        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the queried item.</param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, a null reference is being returned.</returns>
        public static T VisualFindParent<T>(this DependencyObject child)
          where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return VisualFindParent<T>(parentObject);
            }

        }

        public static T VisualFindChild<T>(this DependencyObject parent)
          where T : DependencyObject
        {

            if (parent == null)
                return null;
            int cnt = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < cnt; i++)
            {
                DependencyObject childobject = VisualTreeHelper.GetChild(parent, i);
                if (childobject == null)
                    continue;

                T child = childobject as T;
                if (child != null)
                    return child;
            }

            for (int i = 0; i < cnt; i++)
            {
                DependencyObject childobject = VisualTreeHelper.GetChild(parent, i);
                if (childobject == null)
                    continue;
                var val = VisualFindChild<T>(childobject);
                if (val != null)
                    return val;
            }

            return null;

        }

        public static bool VisualIsVisibleChild(this FrameworkElement parent, FrameworkElement child)
        {
            if (!child.IsVisible)
                return false;
            Rect bounds =
                child.TransformToAncestor(parent).TransformBounds(new Rect(0.0, 0.0, child.ActualWidth, child.ActualHeight));
            var rect = new Rect(0.0, 0.0, parent.ActualWidth, parent.ActualHeight);
            return rect.Contains(bounds.TopLeft) && rect.Contains(bounds.BottomRight);
        }

        public static IEnumerable<T> VisualFindChildren<T>(this DependencyObject parent)
          where T : DependencyObject
        {

            int cnt = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < cnt; i++)
            {
                DependencyObject childobject = VisualTreeHelper.GetChild(parent, i);
                if (childobject == null)
                    continue;
                T child = childobject as T;
                if (child != null)
                    yield return child;
            }

            for (int i = 0; i < cnt; i++)
            {
                DependencyObject childobject = VisualTreeHelper.GetChild(parent, i);
                if (childobject == null)
                    continue;
                var val = VisualFindChildren<T>(childobject);

                foreach (var v in val)
                    yield return v;
            }

        }
    }
}
