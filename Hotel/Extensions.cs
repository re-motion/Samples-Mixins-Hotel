using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Remotion.Mixins;
using Remotion.Reflection;

// This implements cross-cutting concerns and extra features
// for the hotel class. Cross-cutting concerns:
// - security
// - logging
// Extra feature:
// - a reservation queue
namespace Hotel
{
  // ***
  // *** security (authentication/authorization) requires users...
  // *** 
  public class User
  {
    // ... users have a password and may be authorized to make
    // reservations or not. 
    public string Password { get; set; }
    public bool MayMakeReservations { get; set; }

    public User (string password, bool reservations)
    {
      Password = password;
      MayMakeReservations = reservations;
    }
  }

  // ***
  // *** a toy, a prothesis, a cardboard SecMan
  // ***
  public static class SecurityManager
  {
    // Barbara and Manuela are the concierges of our
    // toy hotel. The passwords are probably not very strong,
    // but neither is hardcoding passwds into your program
    // as clear text. 
    static readonly Dictionary<string, User> _authenticationList
      = new Dictionary<string, User> { 
        { "babs", new User ("sbab", false) },    // apprentice, no reservations 
        { "manu", new User ("1234", true) } };   // manager, may make reservations

    // The currently logged on user. Can be babs or manu
    static public string CurrentUser { get; set; }

    // enforce security
    static public bool HasReservationAccess ()
    {
      return _authenticationList[CurrentUser].MayMakeReservations;
    }

    // prompt for password until guessed right. Set current user
    // if right. 
    static public void Login ()
    {
      bool goOn = false;

      do
      {
        string username = null;
        string password = null;

        Console.Write ("   login: ");
        username = Console.ReadLine ();
        Console.Write ("password: ");
        password = Console.ReadLine ();

        if (!(_authenticationList.ContainsKey (username)))
        {
          goOn = true;
          Console.WriteLine ("No such user: {0}", username);
        }
        else
        {
          if (!(password == _authenticationList[username].Password))
          {
            goOn = true;
            Console.WriteLine ("Wrong password");
          }
          else
          {
            CurrentUser = username;
            goOn = false;
          }
        }
      }
      while (goOn);
    }
  }

  // ***
  // *** this is the reservation queue. If no rooms are
  // *** available for a given week, then the reservation is
  // *** stored here. Not very practical without a cancellation 
  // *** facility, but then again, neither is this entire toy
  // *** program
  // ***
  public static class ReservationQueue
  {
    readonly static private List<Reservation> _waiting = new List<Reservation> ();

    static public List<Reservation> Waiting
    {
      get { return _waiting; }
    }

    static public void QueueReservation (int week, string name)
    {
      // A real program would pass 'IReservation' here, but we don't have
      // that
      _waiting.Add (ObjectFactory.Create<Reservation> (ParamList.Create (week, name, -1)));
    }
  }

  // ***
  // *** This is where the rubber meets the road. A diligent enterprise
  // *** programmer extends the 'Hotel's 'MakeReservation' method with
  // *** the queue feature. With mixins he can do that without messing up
  // *** the clarity of the original 'MakeReservation' method.
  // *** This mixin class "intercepts" the original method. 
  // *** 
  [Extends (typeof (Hotel))]
  public class QueueMixin : Mixin<Hotel, IHotel>
  {
    // The improved 'MakeReservation' method, the "interceptor"
    [OverrideTarget]
    public virtual Room MakeReservation (int week, string name)
    {
      try
      {
        // call 'Hotel's 'MakeReservation'
        return Base.MakeReservation (week, name); 
      }
      // if that doesn't work, put the reservation into the queue
      catch (NoRoomException)
      {
        ReservationQueue.QueueReservation (week, name);
      }

      // No room availabe? Return 'null'. 
      return null;
    }
  }

  // ***
  // *** Here we go with security, a cross-cutting concern.
  // *** In a real-working program, many methods will need 
  // *** such security extensions. Again: we can tuck on security
  // *** to the basic (and desired) function of the method
  // *** without messing up the code of that basic function.
  // *** Security has nothing to do with the meat of the software,
  // *** it is a necessary (and "orthogonal") evil. We hope to
  // *** demonstrate the usefulness of mixins for putting cross-cutting
  // *** concerns like these into quarantaine. 
  // ***
  // *** Note 'AdditionalDependencies': It means that this interceptor
  // *** intercepts the interception of the 'QueueMixin' interceptor
  // *** above. 
  [Extends (typeof (Hotel), AdditionalDependencies = new[] { typeof (QueueMixin) })]
  public class AuthorizationMixin : Mixin<Hotel, IHotel>
  {
    [OverrideTarget]
    public virtual Room MakeReservation (int week, string name)
    {
      // if the concierge is authorized to make reservations,
      // just do it... 
      if (SecurityManager.HasReservationAccess ())
      {
        return Base.MakeReservation (week, name);
      }
      else
      {
        // ... if not, blow a fuse
        throw new UnauthorizedAccessException ();
      }
    }
  }

  // ***
  // *** Another interceptor, wrapped around the authorization mixin above.
  // *** Like security, *logging* is another popular example for a cross-cutting concern.
  // *** It has nothing to do with the function of the program, so,
  // *** with mixins, we can keep the logging code separate from the 
  // *** actual domain code. 
  // *** 
  [Extends (typeof (Hotel), AdditionalDependencies = new[] { typeof (AuthorizationMixin) })]
  public class LoggingMixin : Mixin<Hotel, IHotel>
  {
    [OverrideTarget]
    public virtual Room MakeReservation (int week, string name)
    {
      Room returnValue = null;

      // log any attempt to exceed her authority
      try
      {
        returnValue = Base.MakeReservation (week, name);
      }
      catch (UnauthorizedAccessException)
      {
        File.AppendAllText (Program.LogFileName,
          String.Format ("*** User {0} attempted to reserve a room without sufficient privileges ***",
            SecurityManager.CurrentUser));
        throw new UnauthorizedAccessException ();
      }

      // log unsuccessful reservation...
      if (returnValue == null)
      {
        File.AppendAllText (Program.LogFileName,
          String.Format ("Reservation for week {0}, name {1} failed (User {2})\n",
            week, name, SecurityManager.CurrentUser));
      }
      // ... or successful reservation
      else
      {
        File.AppendAllText (Program.LogFileName,
          String.Format ("Reservation: week={0}, name={1} (User {2})\n",
            week, name, SecurityManager.CurrentUser));
      }

      return returnValue;
    }
  }



}
