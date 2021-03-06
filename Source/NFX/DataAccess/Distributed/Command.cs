/*<FILE_LICENSE>
* NFX (.NET Framework Extension) Unistack Library
* Copyright 2003-2014 IT Adapter Inc / 2015 Aum Code LLC
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NFX.DataAccess.Cache;
using System.Collections;

namespace NFX.DataAccess.Distributed
{
    /// <summary>
    /// Defines a command sent into an IDistributedDataStore implementor to retrieve or change(if supported) data.
    /// A Command is a named bag of paremeters where every parameter has a name and a value.
    /// Every command has a unique Identity(GUID) which represents a token of the whole command state (name, type,all params).
    /// The identity is used for quick lookup/caching. The identity may be supplied externally as
    /// business code may map certain parameters into GUID and later reuse the same GUID to retrieve the cached command result, for example
    ///  a web server app may cache command "GetPurchases(user=123, year=2015, month=3)" under session key "MY_PURCHASES_201503" to later
    ///  retrieve a cached (if available) command results from the DB layer, this way the DB server does not have to store the whole
    ///  commands with all params as the cache key (which would have been slow to compare and would have induced GC pressure).
    ///  Warning: DO NOT CACHE command identity value on a client (i.e. web page) in an un-encrypted state, as this is a security flaw
    /// </summary>
    [Serializable]
    public class Command : List<Command.Param>, INamed, IShardingPointerProvider, ICachePolicy
    {
                /// <summary>
                /// Represents a distributed command parameter
                /// </summary>
                [Serializable]
                public sealed class Param 
                {
                    public Param(string name, object value = null)
                    {
                        Name = name ?? string.Empty;
                        Value = value;
                    }


                    public readonly string Name;
                    public readonly object Value;

                    public override string ToString()
                    {
                      return "{0}='{1}'".Args(Name, Value ?? StringConsts.NULL_STRING);
                    }
                }

        
        #region .ctor
            
            public Command(Guid? identity, string name, params Param[] pars )
            {
                m_Identity = identity ?? Guid.NewGuid(); 
                m_Name = name;
                if (pars!=null) AddRange(pars);
            }

            public Command(Guid? identity, string name, Parcel shardingParcel, params Param[] pars ) : this(identity,
                                                                                            name, 
                                                                                            shardingParcel.NonNull(text: "Command.ctor(shardingParcel=null)").ShardingPointer, 
                                                                                            pars)
            {
               
            }

            public Command(Guid? identity, string name, ShardingPointer shardingPtr, params Param[] pars )
            {
                m_Identity = identity ?? Guid.NewGuid(); 

                m_Name = name;
                
                if (!shardingPtr.IsAssigned)
                   throw new DistributedDataAccessException(StringConsts.ARGUMENT_ERROR+ GetType().FullName+".ctor(!shardingPtr.IsAssigned)");
                
                m_ShardingPointer = shardingPtr;

                if (pars!=null) AddRange(pars);
            }
        #endregion
        
        #region Fields
            
            private Guid m_Identity;
            private string m_Name;
            private ShardingPointer m_ShardingPointer;

        #endregion


        #region Properties
            
            
            /// <summary>
            /// Returns the identity of this instance, that is - an ID that UNIQUELY identifies the instance of this command
            /// including all of the names, parameters, values. This is needed for Equality comparison and cache lookup.
            /// The identity is either generated by .ctor or supplied to it if it is cached (i.e. in a user session)
            /// </summary>
            public Guid Identity
            {
               get { return m_Identity;}
            }
            
            
            /// <summary>
            /// Returns Command name, providers use it to locate modules particular to backend implementation that they represent
            /// </summary>
            public virtual string Name
            {
                get { return m_Name ?? string.Empty;}
            }

            /// <summary>
            /// Returns the ShardingPointer for this command
            /// </summary>
            public virtual ShardingPointer ShardingPointer
            {
                get { return m_ShardingPointer; }
            }


            /// <summary>
            /// Returns parameter by its name or null
            /// </summary>
            public Param this[string name]
            {
                get {return this.FirstOrDefault(p => name.EqualsOrdIgnoreCase(p.Name)); }
            }


            /// <summary>
            /// Implements IParcelCachePolicy contract.
            /// The default implementation returns null.
            /// Override to supply a value for maximum length of this isntance stay in cache
            ///  that may depend on particular command state (i.e. param values) 
            /// </summary>
            public virtual int? CacheWriteMaxAgeSec
            {
                get { return null; }
            }

            /// <summary>
            /// Implements IParcelCachePolicy contract.
            /// The default implementation returns null.
            /// Override to supply a value for maximum validity span of cached command data
            ///  that may depend on particular command state (i.e. param values). 
            /// </summary>
            public virtual int? CacheReadMaxAgeSec
            {
                get { return null; }
            }

            /// <summary>
            /// Implements IParcelCachePolicy contract.
            /// The default implementation returns null.
            /// Override to supply a relative cache priority of this command data
            ///  that may depend on particular command state (i.e. param values). 
            /// </summary>
            public virtual int? CachePriority
            {
                get { return null; }
            }

            /// <summary>
            /// Implements IParcelCachePolicy contract.
            /// The implementation returns null for commands
            /// </summary>
            string ICachePolicy.CacheTableName
            {
                get { return null; }
            }

            /// <summary>
            /// Implements IParcelCachePolicy contract.
            /// The default implementation returns null.
            /// Override to supply a different absolute cache expiration UTC timestamp for this command data
            ///  that may depend on particular command state (i.e. field values). 
            /// </summary>
            public DateTime? CacheAbsoluteExpirationUTC
            {
                get { return null; }
            }
            
       #endregion

       #region Public
            

            public override string ToString()
            {
                return "{0}[{1}]('{2}')`{3}".Args(GetType().Name, m_Identity, Name, this.Count());
            }


            public override bool Equals(object obj)
            {
              var other = obj as Command;
              if (other==null) return false;
                           
              return this.m_Identity == other.Identity;
            }

            public override int GetHashCode()
            {
              return this.m_Identity.GetHashCode();
            }

       #endregion
            
    }


    /// <summary>
    /// Represents a result of command that is absent. This is needed to distinguish from null reference
    /// </summary>
    public sealed class NullCommandResult
    {
      public static readonly NullCommandResult Instance = new NullCommandResult();

      private NullCommandResult(){}

      public override bool Equals(object obj)
      {
        return obj is NullCommandResult;
      }

      public override string ToString()
      {
        return "[NULL COMMAND RESULT]";
      }

      public override int GetHashCode()
      {
        return 0;
      }
    }



}
