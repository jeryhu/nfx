﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFX.Serialization.Slim
{
  /// <summary>
  /// When set on a parameterless constructor, instructs the Slim serializer not to invoke
  ///  the ctor() on deserialization
  /// </summary>
  [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
  public class SlimDeserializationCtorSkipAttribute : Attribute
  {
    public SlimDeserializationCtorSkipAttribute(){ }
  }
}
