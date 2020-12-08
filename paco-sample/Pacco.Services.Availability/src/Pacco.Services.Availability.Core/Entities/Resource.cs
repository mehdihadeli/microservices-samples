using System;
using System.Collections.Generic;
using System.Linq;
using Pacco.Services.Availability.Core.Events;
using Pacco.Services.Availability.Core.Exceptions;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Core.Entities
{
    public class Resource : AggregateRoot  // resource is like a diver
    {
        // we use set because ordering is not important
        private ISet<string> _tags = new HashSet<string>(); // resource can be different here we tag it to vehicle
        private ISet<Reservation> _reservations = new HashSet<Reservation>(); // list of reservation that we made for this resource

        public IEnumerable<string> Tags
        {
            get => _tags;
            private set => _tags = new HashSet<string>(value);
        }

        // reservation can be a value object or entity in our perspective it is value object and we don't care about its identity
        public IEnumerable<Reservation> Reservations
        {
            get => _reservations;
            private set => _reservations = new HashSet<Reservation>(value);
        }

        // when we create resource it can have no reservation, we can add it later
        // if creation of object is complex we can extract it in a factory.
        public Resource(Guid id, IEnumerable<string> tags, IEnumerable<Reservation> reservations = null,
            int version = 0)
        {
            //here we need to sure we never violate our invariant
            ValidateTags(tags);
            Id = id;
            Tags = tags;
            Reservations = reservations ?? Enumerable.Empty<Reservation>();
            Version = version;
        }

        private static void ValidateTags(IEnumerable<string> tags)
        {
            if (tags is null || !tags.Any())
            {
                throw new MissingResourceTagsException();
            }

            if (tags.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidResourceTagsException();
            }
        }

        // we create our factory method here because we want to accese add event method an it is protected and only accessible here

        // we create a method for creating resource we can use constructor also, for new resource we use factory method and one we will
        // let say fetch database and we want to resore this as an aggregate and we will use constructore and because the constructor itself
        // don't have a event and we don't need it for creating resource here
        public static Resource Create(Guid id, IEnumerable<string> tags, IEnumerable<Reservation> reservations = null)
        {
            var resource = new Resource(id, tags, reservations);
            // we want to publish this domain event internally
            resource.AddEvent(new ResourceCreated(resource));
            return resource;
        }

        public void AddReservation(Reservation reservation)
        {
            var hasCollidingReservation = _reservations.Any(HasTheSameReservationDate);
            if (hasCollidingReservation)
            {
                var collidingReservation = _reservations.First(HasTheSameReservationDate);
                if (collidingReservation.Priority >= reservation.Priority)
                {
                    throw new CannotExpropriateReservationException(Id, reservation.DateTime.Date);
                }

                if (_reservations.Remove(collidingReservation))
                {
                    AddEvent(new ReservationCanceled(this, collidingReservation));
                }
            }

            if (_reservations.Add(reservation))
            {
                AddEvent(new ReservationAdded(this, reservation));
            }

            bool HasTheSameReservationDate(Reservation r) => r.DateTime.Date == reservation.DateTime.Date;
        }

        public void ReleaseReservation(Reservation reservation)
        {
            if (!_reservations.Remove(reservation))
            {
                return;
            }

            AddEvent(new ReservationReleased(this, reservation));
        }

        public void Delete()
        {
            foreach (var reservation in Reservations)
            {
                AddEvent(new ReservationCanceled(this, reservation));
            }

            AddEvent(new ResourceDeleted(this));
        }
    }
}