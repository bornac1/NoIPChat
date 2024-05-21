using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    public readonly struct Version : IComparable<Version>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        private readonly string version;
        public Version(string version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version), "Version string cannot be null.");
            }
            var parts = version.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Version string must be in the format 'major.minor.patch'.", nameof(version));
            }
            if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor) || !int.TryParse(parts[2], out int patch))
            {
                throw new ArgumentException("Version parts must be integers.", nameof(version));
            }
            Major = major;
            Minor = minor;
            Patch = patch;
            this.version = version;
        }
        public Version (int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            version = major.ToString() + "." + minor.ToString() + "." + patch.ToString();
        }
        public static implicit operator Version(string version)
        {
            return new Version(version);
        }
        public int CompareTo(Version other)
        {
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            return Patch.CompareTo(other.Patch);
        }
        public override bool Equals(object? obj)
        {
            if (obj is Version other)
            {
                return Major == other.Major &&
                       Minor == other.Minor &&
                       Patch == other.Patch;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }
        public static bool operator >(Version v1, Version v2)
        {
            return v1.CompareTo(v2) > 0;
        }
        public static bool operator <(Version v1, Version v2)
        {
            return v1.CompareTo(v2) < 0;
        }
        public static bool operator >=(Version v1, Version v2)
        {
            return v1.CompareTo(v2) >= 0;
        }
        public static bool operator <=(Version v1, Version v2)
        {
            return v1.CompareTo(v2) <= 0;
        }
        public static bool operator ==(Version v1, Version v2)
        {
            return v1.Equals(v2);
        }
        public static bool operator !=(Version v1, Version v2)
        {
            return !(v1 == v2);
        }
        public override string ToString()
        {
            return version;
        }
        public static implicit operator string(Version version)
        {
            return version.ToString();
        }
    }
}
