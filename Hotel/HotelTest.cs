
using NUnit.Core;
using NUnit.Framework;
using Remotion.Implementation;
using Remotion.Mixins;
using Remotion.Reflection;

namespace Hotel
{
  [TestFixture]
  public class HotelTest
  {
    Hotel _hotel = null;

    [SetUp]
    public void SetUp ()
    {
      SecurityManager.CurrentUser = "manu";
      _hotel = ObjectFactory.Create<Hotel> (ParamList.Empty);

    }

    [Test]
    public void TestFindRoom ()
    {
      for (int iter = 0; iter < Hotel.NumberOfRooms - 1; iter++)
      {
        _hotel.MakeReservation (0, "disco-fred");
      }

      Assert.AreEqual (_hotel.FindFreeRoom (0).Number, Hotel.NumberOfRooms - 1);
    }

    [Test]
    public void TestDontFindRoom ()
    {
      SecurityManager.CurrentUser = "manu";

      for (int iter = 0; iter < Hotel.NumberOfRooms; iter++)
      {
        _hotel.MakeReservation (0, "disco-fred");
      }

      var room = _hotel.FindFreeRoom (0);
      Assert.AreEqual (room, null);
    }

    [Test]
    public void TestRoomReserveFree ()
    {
      var room = ObjectFactory.Create<Room> (ParamList.Create (0));
      Assert.IsTrue (room.IsFree (0));
      room.MakeReservation (0, "disco-fred");
      Assert.IsFalse (room.IsFree (0));
    }

    [Test]
    [ExpectedException ("Hotel.AlreadyBookedException")]
    public void TestRoomReserveException ()
    {
      var room = ObjectFactory.Create<Room> (ParamList.Create (3));
      room.MakeReservation (0, "disco-fred");
      room.MakeReservation (0, "afro-bob");
    }

    [Test]
    public void TestMakeGetReservation ()
    {
      SecurityManager.CurrentUser = "manu";
      _hotel = ObjectFactory.Create<Hotel> (ParamList.Empty);
      var reservations = _hotel.GetAllReservations ();

      Assert.AreEqual (reservations.Count, 0);

      var room = _hotel.MakeReservation (0, "disco-fred");

      reservations = _hotel.GetAllReservations ();
      Assert.AreEqual (reservations.Count, 1);

      Assert.AreEqual (room.GetReservations ()[0].Number, 0);

      Assert.IsFalse (room.IsFree (0));
      Assert.AreEqual (room.GetReservations ()[0].Name, "disco-fred");
    }

    [Test]
    public void TestExhaustReservations ()
    {
      SecurityManager.CurrentUser = "manu";
      _hotel = ObjectFactory.Create<Hotel> (ParamList.Empty);

      for (int iter = 0; iter < Hotel.NumberOfRooms; iter++)
      {
        _hotel.MakeReservation (0, "disco-fred");
      }

      Assert.AreEqual (_hotel.GetAllReservations ().Count, Hotel.NumberOfRooms);

      var room = _hotel.MakeReservation (0, "afro-bob");
      Assert.AreEqual (room, null);
    }

    [Test]
    [ExpectedException ("System.UnauthorizedAccessException")]
    public void TestReservationViolation ()
    {
      SecurityManager.CurrentUser = "babs";
      _hotel = ObjectFactory.Create<Hotel> (ParamList.Empty);

      _hotel.MakeReservation (0, "disco-fred");

    }
  }
}