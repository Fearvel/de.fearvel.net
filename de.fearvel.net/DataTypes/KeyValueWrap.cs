﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using de.fearvel.net.DataTypes.AbstractDataTypes;
using Newtonsoft.Json;

namespace de.fearvel.net.DataTypes
{
    public class KeyValueWrap : JsonSerializable<KeyValueWrap>
    {
        public string Key;
        public string Val;
    }
}
