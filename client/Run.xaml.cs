using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.AccessControl;

namespace specify_client
{
    /*
     
    Hey future programmers!
    
    This is the code-behind of the program when its actually run. We'd actually like to add text where
    it displays its current task in Progress.cs. However, I wasted over 20 hours trying to figure out how.

    I know the GUI stuff is completely trash and unoptimized, mostly because no one wanted to deal with 
    XAML which is fair, so I had to deal with it. But if you'd like to try to convert all of this into 
    MVVM or structure the entire UI better, go ahead!
    
    Still here? Alright, here's how I tried to do it:

    Progress.cs contains all of the names of the task under item.Name, so I put that into a string, and 
    sent it off to here in Run.xaml.cs in a method called StatusUpdate(string status). We can't willy nilly 
    change the TextBlock (x:Name = "StatusText") because the entirety of the program past Progress.cs is
    in another thread, so you have to force feed the UI thread with StatusText.Text = status.

    The problem is none of the usual methods have worked.

    Using the code from Landing.cs for the Frame stuff doesn't work because you'd need to pass the string
    to it, so I tried using Cache.cs as a hail mary. It didn't update and forced the program to max CPU util.

    App.Current.Dispatcher froze the window as well.

    If you somehow made this work without compromise, then I'll be damned.

                                                                                        - K97i
    */
    public partial class Run : Page
    {
        public Run()
        {
            InitializeComponent();

            Program.Main();
        }
    }
}
