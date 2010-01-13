using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Remotion.Implementation;
using Remotion.Mixins;
using Remotion.Reflection;

// *** 
// *** A simple TTY-frontend for our toy hotel program.
// *** For entertainment purposes only
// *** 

namespace Hotel
{
  // ***
  // *** This class parses command line input and acts 
  // *** accordingly. 
  // *** 
  public class UiDispatcher
  {
    readonly Hotel _hotel = null;
    readonly string _logFileName = null;

    public UiDispatcher (Hotel hotel, string logFileName)
    {
      _hotel = hotel;
      _logFileName = logFileName;
    }

    // help screen
    public void ShowOptions ()
    {
      Console.WriteLine ();
      Console.WriteLine ("Options:");
      Console.WriteLine ("  r Reservation week name");
      Console.WriteLine ("  l List reservations");
      Console.WriteLine ("  q Show queue");
      Console.WriteLine ("  ? This help");
      Console.WriteLine ("  . Quit");
      Console.WriteLine ();
    }

    // we parse command line input with regular expressions 
    readonly static Regex reservationRegex = new Regex (@"[ \t]*r[ \t]+(?<week>\d+)[ \t](?<name>[^ \t]+)");
    readonly static Regex bogusReservationRegex = new Regex ("@[ \t]*r");
    readonly static Regex listRegex = new Regex (@"[ \t]*l");
    readonly static Regex queueRegex = new Regex (@"[ \t]*q");
    readonly static Regex helpRegex = new Regex (@"[ \t]*\?");
    readonly static Regex quitRegex = new Regex (@"[ \t]*\.");

    // return true if the command line matches a COMPLETE reservation
    // command and act accordingly. 
    public bool DispatchReservation (string line)
    {
      var match = reservationRegex.Match (line);
      if (!(match == Match.Empty))
      {
        var weekString = match.Groups["week"].ToString ();
        var week = int.Parse (weekString);
        if (week >= Room.WeeksInYear)
        {
          Console.WriteLine ("There are only {0} weeks in the year (first week: 0)", Room.WeeksInYear);
          return true;
        }

        var name = match.Groups["name"].ToString ();

        Room reservedRoom = null;

        try
        {
          reservedRoom = _hotel.MakeReservation (week, name);

          if (reservedRoom == null)
          {
            Console.WriteLine ("No more rooms for week {0}! Putting reservation into queue.", week);
          }
          else
          {
            Console.WriteLine ("Room {0} reserved for week {1}", reservedRoom.Number, week);
          }

          return true;
        }
        catch (UnauthorizedAccessException)
        {
          Console.WriteLine ("*** User {0} not authorized to make reservations. ***\n");
        }
      }
      return false;
    }

    // return true if the given line matches an INCOMPLETE reservation
    // command and notify the user
    public bool DispatchBogusReservation (string line)
    {
      var match = bogusReservationRegex.Match (line);
      if (!(match == Match.Empty))
      {
        Console.WriteLine ("reservations require two parameters: a week (integer, starting with 0) and a name");
      }
      return false;
    }


    // returns true if the given line matches a list (reservations) command
    // and acts accordingly
    public bool DispatchList (string line)
    {
      var match = listRegex.Match (line);
      if (!(match == Match.Empty))
      {
        Console.WriteLine ();
        Console.WriteLine ("Room Week Name");
        Console.WriteLine ("==============");
        foreach (var reservation in _hotel.GetAllReservations ())
        {
          Console.WriteLine ("{0,04:D}  {1,02:D}  {2}", reservation.Number, reservation.Week, reservation.Name);
        }
        return true;
      }
      return false;
    }

    // returns true if the given line matches a 'list reservations in queue' 
    // command and acts accordingly
    public bool DispatchQueue (string line)
    {
      var match = queueRegex.Match (line);
      if (!(match == Match.Empty))
      {
        Console.WriteLine ();
        Console.WriteLine ("Week Name");
        Console.WriteLine ("=========");
        foreach (var reservation in ReservationQueue.Waiting)
        {
          Console.WriteLine (" {0,02:D}  {1}", reservation.Week, reservation.Name);
        }
        return true;
      }
      return false;
    }

    // returns true if the given line matches a 'show help screen' command
    // and acts accordingly
    public bool DispatchHelp (string line)
    {
      var match = helpRegex.Match (line);
      if (!(match == Match.Empty))
      {
        ShowOptions ();
        return true;
      }
      return false;
    }

    // returns true if the given line matches a 'quit application' command
    // and acts accordingly
    public bool DispatchQuit (string line)
    {
      var match = quitRegex.Match (line);
      if (!(match == Match.Empty))
      {
        return true;
      }
      return false;
    }
  }

  // ***
  // *** Sets up the system, prompts for a login and 
  // *** uses a 'UiDispatcher' instance to process commands
  // ***
  class Program
  {
    public const string LogFileName = "Hotel.log";

    static void Main (string[] args)
    {
      if (!File.Exists (LogFileName))
      {
        File.Create (LogFileName);
      }

      // you must force the .NET runtime to load a reference to the
      // Remotion.dll assembly. Otherwise the compiler will remove the
      // facilities for loading the Remotion.dll assembly and your application
      // will throw a type initializer exception, because it can't load it.
      // The easiest way to touch the assembly is to use the IMixinTarget
      // identifier:
      FrameworkVersion.RetrieveFromType (typeof (IMixinTarget));

      // In contrast to the other instantiations of domain classes (Room, Reservation),
      // we MUST use the object factory here, for the mixins to work. 
      var hotel = ObjectFactory.Create<Hotel> (ParamList.Empty);

      var uiDispatcher = new UiDispatcher (hotel, LogFileName);

      SecurityManager.Login ();

      // for your convenience -- no head-scratching
      uiDispatcher.ShowOptions ();

      for (; ; )
      {
        Console.Write ("> ");
        string line = Console.ReadLine ();

        if (String.IsNullOrEmpty (line))
        {
          continue;
        }

        if (uiDispatcher.DispatchBogusReservation (line)) 
          continue;
        else if (uiDispatcher.DispatchReservation (line))
          continue;
        else if (uiDispatcher.DispatchList (line))
          continue;
        else if (uiDispatcher.DispatchQueue (line))
          continue;
        else if (uiDispatcher.DispatchHelp (line))
          continue;
        else if (uiDispatcher.DispatchQuit (line))
          break;
        else
          Console.WriteLine ("Bogus input");
      }
    }
  }
}
