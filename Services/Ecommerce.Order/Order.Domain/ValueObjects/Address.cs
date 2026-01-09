using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Domain.ValueObjects
{
    public class Address
    {
        public string UserName { get; } = default!;
        public string? EmailAddress { get; } = default!;
        public string AddressLine { get; } = default!;
        public string State { get; } = default!;
        public string ZipCode { get; } = default!;


        protected Address()
        {
        }

        public Address(string userName, string emailAddress, string addressLine, string state, string zipCode)
        {
            UserName = userName;
            EmailAddress = emailAddress;
            AddressLine = addressLine;

            State = state;
            ZipCode = zipCode;
        }

        public static Address Of(string userName, string emailAddress, string addressLine,  string state, string zipCode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
            ArgumentException.ThrowIfNullOrWhiteSpace(addressLine);

            return new Address(userName, emailAddress, addressLine,  state, zipCode);
        }
    }
}
