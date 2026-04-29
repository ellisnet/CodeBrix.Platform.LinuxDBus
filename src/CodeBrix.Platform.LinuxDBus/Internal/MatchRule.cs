using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace CodeBrix.Platform.LinuxDBus; //was previously: Tmds.DBus.Protocol;
struct MatchRuleData
{
    public MessageType? MessageType { get; set; }

    public string Sender { get; set; }

    public string Interface { get; set; }

    public string Member { get; set; }

    public string Path { get; set; }

    public string PathNamespace { get; set; }

    public string Destination { get; set; }

    public string Arg0 { get; set; }

    public string Arg0Path { get; set; }

    public string Arg0Namespace { get; set; }

    public string GetRuleString()
    {
        var sb = new StringBuilder(); // TODO (perf): pool

        if (MessageType.HasValue)
        {
            string typeMatch = MessageType switch
            {
                global::CodeBrix.Platform.LinuxDBus.MessageType.MethodCall => "type=method_call",
                global::CodeBrix.Platform.LinuxDBus.MessageType.MethodReturn => "type=method_return",
                global::CodeBrix.Platform.LinuxDBus.MessageType.Error => "type=error",
                global::CodeBrix.Platform.LinuxDBus.MessageType.Signal => "type=signal",
                _ => null
            };

            if (typeMatch is not null)
            {
                sb.Append(typeMatch);
            }
        }

        Append(sb, "sender", Sender);
        Append(sb, "interface", Interface);
        Append(sb, "member", Member);
        Append(sb, "path", Path);
        Append(sb, "pathNamespace", PathNamespace);
        Append(sb, "destination", Destination);
        Append(sb, "arg0", Arg0);
        Append(sb, "arg0Path", Arg0Path);
        Append(sb, "arg0Namespace", Arg0Namespace);

        return sb.ToString();

        static void Append(StringBuilder sb, string key, string value)
        {
            if (value is null)
            {
                return;
            }

            sb.Append($"{(sb.Length > 0 ? ',' : "")}{key}=");

            bool quoting = false;

            ReadOnlySpan<char> span = value.AsSpan();
            while (!span.IsEmpty)
            {
                int specialPos = span.IndexOfAny((ReadOnlySpan<char>)new char[] { ',', '\'' });
                if (specialPos == -1)
                {
                    sb.Append(span);
                    break;
                }
                bool isComma = span[specialPos] == ',';
                if (isComma && !quoting ||
                    !isComma && quoting)
                {
                    sb.Append("'");
                    quoting = !quoting;
                }
                sb.Append(span.Slice(0, specialPos + (isComma ? 1 : 0)));
                if (!isComma)
                {
                    sb.Append("\\'");
                }
                span = span.Slice(specialPos + 1);
            }

            if (quoting)
            {
                sb.Append("'");
                quoting = false;
            }
        }
    }
}

/// <summary>Match Rule.</summary>
public sealed class MatchRule
{
    private MatchRuleData _data;

    internal MatchRuleData Data => _data;

    /// <summary>Type.</summary>
    public MessageType? Type { get => _data.MessageType; set => _data.MessageType = value; }
    /// <summary>Sender.</summary>
    public string Sender { get => _data.Sender; set => _data.Sender = value; }
    /// <summary>Interface.</summary>
    public string Interface { get => _data.Interface; set => _data.Interface = value; }
    /// <summary>Member.</summary>
    public string Member { get => _data.Member; set => _data.Member = value; }
    /// <summary>Path.</summary>
    public string Path { get => _data.Path; set => _data.Path = value; }
    /// <summary>Path Namespace.</summary>
    public string PathNamespace { get => _data.PathNamespace; set => _data.PathNamespace = value; }
    /// <summary>Destination.</summary>
    public string Destination { get => _data.Destination; set => _data.Destination = value; }
    /// <summary>Arg0.</summary>
    public string Arg0 { get => _data.Arg0; set => _data.Arg0 = value; }
    /// <summary>Arg0 Path.</summary>
    public string Arg0Path { get => _data.Arg0Path; set => _data.Arg0Path = value; }
    /// <summary>Arg0 Namespace.</summary>
    public string Arg0Namespace { get => _data.Arg0Namespace; set => _data.Arg0Namespace = value; }

    /// <summary>To String.</summary>
    public override string ToString() => _data.GetRuleString();
}