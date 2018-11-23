﻿using Biob.Data.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Biob.Data.Models
{
    [Table("HallSeats")]
    public class HallSeat : DeleteableModelBase<int>
    {
        [ForeignKey("Hall")]
        public int HallId { get; set; }
        public Hall Hall { get; set; }
        [ForeignKey("Seat")]
        public int SeatId { get; set; }
        public Seat Seat { get; set; }
    }
}
