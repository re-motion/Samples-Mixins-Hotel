using System;
using System.Collections.Generic;
using Remotion.Mixins;
using Remotion.Reflection;

// This is the basic implementation of 'Hotel', based on the
// example from Ivar Jacobson's and Pan Wei-Ng's book 
// 
//    *Aspect-oriented development with use-cases*
//
// (Cross-cutting) concerns like logging and security are
// implemented in 'Extensions.cs'.
//
// Only the fragment dealing with reservation of rooms is
// implemented here. A full-blown concierge system will 
// also implement checking in and out of guests, billing,
// complaints, etc.
// To keep things simple, we will introduce several 
// simplifications (after all, this program is a toy):
// - rooms can only be booked on a weekly basis (and
//   every year has 52 weeks)
// - you can't reserve a specific room, like "number 7, the
//   we had for our honeymoon in 1958", only SOME room
// - cancellation of a reservation is not supported here
// - there is just one type of room; real hotels would have
//   - 'value rooms' (no cable TV, shared bathroom in the hallway)
//   - 'premium rooms' (basic cable, own bathroom), 
//   - 'royal rooms' (suite with premium cable)
// - not all domain classes like 'Reservation', 'Room', etc.
//   provide interfaces (as a real well-engineered C# program
//   would)

namespace Hotel
{
  // Only one booking per room and week, for obvious reasons:
  public class AlreadyBookedException : Exception { }

  // No more rooms available for a given week? Throw this
  // exception:
  public class NoRoomException : Exception { }

  // ***
  // *** A complete reservation info. No code, just data
  // ***
  public class Reservation
  {
    public int Week { get; set; }
    public string Name { get; set; }
    public int Number { get; set; }

    public Reservation (int week, string name, int number)
    {
      Week = week;
      Name = name;
      Number = number;
    }
  }

  // ***
  // *** The room class -- responsibilities are explained as we go.
  // ***
  public class Room
  {
    // 52 weeks is hard to exhaust in a spin around
    // the block. We use '2 weeks' instead for convenient
    // trial runs.
    // const int WeeksInYear = 52; // comment this in for correctness
    public const int WeeksInYear = 2;
    // Introduced for completeness, not used by this sample
    public const int RoomFare = 500;

    // A room stores who has booked the room for each week
    // The string is the name of the person who has reserved
    // the room. 'null' means that the room is free for the
    // given week.
    readonly private string[] _reservations = new string[WeeksInYear];

    // Find all non-null values in the reservation array above =
    // all the weeks when the room is reserved. 
    public IList<Reservation> GetReservations()
    {
      var resultList = new List<Reservation>();
      for (int weekIter = 0; weekIter < Room.WeeksInYear; weekIter++)
      {
        if (!IsFree (weekIter))
          resultList.Add (ObjectFactory.Create<Reservation> (ParamList.Create (weekIter, _reservations[weekIter], Number)));
      }
      return resultList;
    }

    // The room's number.
    readonly private int _number;
    public int Number { get { return _number; } }

    // Just for completeness, not used
    readonly private int _fare;
    public int Fare { get { return _fare; } }

    public Room (int number)
    {
      _fare = RoomFare;
      _number = number;
    }

    // reserve the room
    public Room MakeReservation (int week, string name)
    {
      if (IsFree (week))
      {
        _reservations[week] = name;
      }
      else
      {
        throw new AlreadyBookedException ();
      }
      return this;
    }

    public bool IsFree (int week)
    {
      return _reservations[week] == null;
    }
  }

  // ***
  // *** we need this interface for mixing (see 'Extensions.cs')
  // ***
  public interface IHotel
  {
    Room GetRoom (int number);
    Room FindFreeRoom (int week);
    Room MakeReservation (int week, string name);
    ICollection<Reservation> GetAllReservations ();
  }

  // ***
  // *** This is the top-level of this domain implementation
  // *** It bolts together the mechanics
  // ***
  public class Hotel : IHotel
  {
    // 2 rooms are easier to play with than 12
    // 12 are more realistic for a hotel, however
    // public const int NumberOfRooms = 12; 
    public const int NumberOfRooms = 2;

    // The rooms in our hotel 
    readonly List<Room> _rooms;

    // Initializes the rooms
    public Hotel ()
    {
      _rooms = new List<Room> ();
      for (int iter = 0; iter < NumberOfRooms; iter++)
      {
        // You'd pass 'IRoom' here in a real program. We
        // go the fast and easy route here
        _rooms.Add (ObjectFactory.Create<Room> (ParamList.Create (iter))); 
      }
    }

    public virtual Room GetRoom (int number)
    {
      return _rooms[number];
    }

    // Find a free room for the given week. 
    public virtual Room FindFreeRoom (int week)
    {
      foreach (var room in _rooms)
      {
        if (room.IsFree (week))
        {
          return room;
        }
      }
      return null;
    }

    // Reserve SOME room for the given week under the
    // given name, like "Smith for week 5".
    // As we will see in 'Extensions.cs', this is not as
    // trivial as it appears to be here. But that's exactly the
    // point of separating concerns: Don't let security or logging
    // distract from the essential function of a method.
    // Note that we throw an exception if no more rooms are
    // available. This will be important in 'Extensions.cs'. 
    public virtual Room MakeReservation (int week, string name)
    {
      var foundRoom = FindFreeRoom (week);
      if (foundRoom == null)
      {
        throw new NoRoomException ();
      }
      else
      {
        foundRoom.MakeReservation (week, name);
        return foundRoom;
      }
    }

    // find reservations for all rooms and all weeks
    public ICollection<Reservation> GetAllReservations ()
    {
      var resultList = new List<Reservation> ();

      foreach (var room in _rooms)
      {
        foreach (var reservation in room.GetReservations())
        {
          resultList.Add (reservation);
        }
      }

      return resultList;
    }
  }
}
