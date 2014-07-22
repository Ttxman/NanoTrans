using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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


        public static object GetObjectAtPoint<ItemContainer>(this ItemsControl control, Point p)
                                     where ItemContainer : DependencyObject
        {
            // ItemContainer - can be ListViewItem, or TreeViewItem and so on(depends on control)
            ItemContainer obj = GetContainerAtPoint<ItemContainer>(control, p);
            if (obj == null)
                return null;

            return control.ItemContainerGenerator.ItemFromContainer(obj);
        }

        public static ItemContainer GetContainerAtPoint<ItemContainer>(this ItemsControl control, Point p)
                                 where ItemContainer : DependencyObject
        {
            HitTestResult result = VisualTreeHelper.HitTest(control, p);
            DependencyObject obj = result.VisualHit;

            while (VisualTreeHelper.GetParent(obj) != null && !(obj is ItemContainer))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            // Will return null if not found
            return obj as ItemContainer;
        }
    }



    /// <summary>
    /// run task synchronously
    /// from:http://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously
    /// </summary>
    public static class AsyncHelpers
    {
        /// <summary>
        /// Execute's an async Task<T> method which has a void return value synchronously
        /// </summary>
        /// <param name="task">Task<T> method to execute</param>
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            synch.Post(async _ =>
            {
                try
                {
                    await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();

            SynchronizationContext.SetSynchronizationContext(oldContext);
        }

        /// <summary>
        /// Execute's an async Task<T> method which has a T return type synchronously
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="task">Task<T> method to execute</param>
        /// <returns></returns>
        public static T RunSync<T>(Func<Task<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            T ret = default(T);
            synch.Post(async _ =>
            {
                try
                {
                    ret = await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();
            SynchronizationContext.SetSynchronizationContext(oldContext);
            return ret;
        }

        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool done;
            public Exception InnerException { get; set; }
            readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
            readonly Queue<Tuple<SendOrPostCallback, object>> items =
                new Queue<Tuple<SendOrPostCallback, object>>();

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (items)
                {
                    items.Enqueue(Tuple.Create(d, state));
                }
                workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => done = true, null);
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    Tuple<SendOrPostCallback, object> task = null;
                    lock (items)
                    {
                        if (items.Count > 0)
                        {
                            task = items.Dequeue();
                        }
                    }
                    if (task != null)
                    {
                        task.Item1(task.Item2);
                        if (InnerException != null) // the method threw an exeption
                        {
                            throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                        }
                    }
                    else
                    {
                        workItemsWaiting.WaitOne();
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }

}