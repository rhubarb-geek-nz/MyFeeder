/**************************************************************************
 *
 *  Copyright 2015, Roger Brown
 *
 *  This file is part of Roger Brown's MyFeeder.
 *
 *  This program is free software: you can redistribute it and/or modify it
 *  under the terms of the GNU Lesser General Public License as published by the
 *  Free Software Foundation, either version 3 of the License, or (at your
 *  option) any later version.
 * 
 *  This program is distributed in the hope that it will be useful, but WITHOUT
 *  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 *  FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for
 *  more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>
 *
 */

/* 
 * $Id: PaymentAddress.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Runtime.Serialization;

namespace MyFeeder
{
    [DataContract]
    public class PaymentAddress
    {
        [DataMember(IsRequired = true, Name = "country")]
        public string Country { get; set; }
        [DataMember(IsRequired = true, Name = "addressLine")]
        public string [] AddressLine { get; set; }
        [DataMember(IsRequired = true, Name ="region")]
        public string Region { get; set; }
        [DataMember(IsRequired = true, Name = "city")]
        public string City { get; set; }
        [DataMember(IsRequired = true, Name = "dependentLocality")]
        public string DependentLocality { get; set; }
        [DataMember(IsRequired = true, Name = "postalCode")]
        public string PostalCode { get; set; }
        [DataMember(IsRequired = true, Name = "sortingCode")]
        public string SortingCode { get; set; }
        [DataMember(IsRequired = true, Name = "languageCode")]
        public string LanguageCode { get; set; }
        [DataMember(IsRequired = true, Name = "organization")]
        public string Organization { get; set; }
        [DataMember(IsRequired = true, Name = "recipient")]
        public string Recipient { get; set; }
        [DataMember(IsRequired = true, Name = "phone")]
        public string Phone { get; set; }
    }
}
