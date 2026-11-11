using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// Stores <see cref="ulong"/> and nullable <see cref="ulong"/> members as BSON Decimal128. The driver's
    /// default is Int64 with an overflow check, which rejects every value above <see cref="long.MaxValue"/>;
    /// Decimal128 holds the whole range up to <see cref="ulong.MaxValue"/> and keeps numeric ordering.
    /// </summary>
    /// <remarks>
    /// Documents written with the default representation stay readable: the serializer reads Int64 as well as
    /// Decimal128, and the server compares the two numerically, so a filter or a unique index treats a stored
    /// Int64 7 and a Decimal128 7 as the same value.
    /// </remarks>
    public class MongoUnsignedLongConvention : ConventionBase, IMemberMapConvention
    {
        /// <summary>The name the convention pack is registered under.</summary>
        public const string PackName = "Bam.Data.Repositories unsigned long as Decimal128";

        private static readonly object _registerLock = new object();
        private static bool _registered;

        /// <summary>
        /// Registers the convention for every type, once per process. It applies to class maps created after
        /// the call; <see cref="MongoRepository"/> calls it before it maps anything.
        /// </summary>
        public static void Register()
        {
            lock (_registerLock)
            {
                if (_registered)
                {
                    return;
                }

                ConventionPack pack = new ConventionPack();
                pack.Add(new MongoUnsignedLongConvention());
                ConventionRegistry.Register(PackName, pack, type => true);
                _registered = true;
            }
        }

        /// <inheritdoc />
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType == typeof(ulong))
            {
                memberMap.SetSerializer(new UInt64Serializer(BsonType.Decimal128));
            }
            else if (memberMap.MemberType == typeof(ulong?))
            {
                memberMap.SetSerializer(new NullableSerializer<ulong>(new UInt64Serializer(BsonType.Decimal128)));
            }
        }
    }
}
