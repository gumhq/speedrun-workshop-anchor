using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using BagboxSession;
using BagboxSession.Program;
using BagboxSession.Errors;
using BagboxSession.Accounts;

namespace BagboxSession
{
    namespace Accounts
    {
        public partial class Player
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 15766710478567431885UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{205, 222, 112, 7, 165, 155, 206, 218};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "bSBoKNsSHuj";
            public PublicKey Authority { get; set; }

            public static Player Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Player result = new Player();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }

        public partial class Bag
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 7475035319352707537UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{209, 73, 132, 189, 4, 169, 188, 103};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "c1MB62xzkcv";
            public PublicKey Player { get; set; }

            public byte Damage { get; set; }

            public static Bag Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Bag result = new Bag();
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.Damage = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum BagboxSessionErrorKind : uint
        {
            InvalidSessionToken = 6000U
        }
    }

    public partial class BagboxSessionClient : TransactionalBaseClient<BagboxSessionErrorKind>
    {
        public BagboxSessionClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>> GetPlayersAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Player.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res);
            List<Player> resultingAccounts = new List<Player>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Player.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Bag>>> GetBagsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Bag.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Bag>>(res);
            List<Bag> resultingAccounts = new List<Bag>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Bag.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Bag>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Player>> GetPlayerAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res);
            var resultingAccount = Player.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Bag>> GetBagAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Bag>(res);
            var resultingAccount = Bag.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Bag>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Player> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Player parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Player.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeBagAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Bag> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Bag parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Bag.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.BagboxSessionProgram.Initialize(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAttackAsync(AttackAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.BagboxSessionProgram.Attack(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<BagboxSessionErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<BagboxSessionErrorKind>>{{6000U, new ProgramError<BagboxSessionErrorKind>(BagboxSessionErrorKind.InvalidSessionToken, "The session token is invalid")}, };
        }
    }

    namespace Program
    {
        public class InitializeAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey Bag { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AttackAccounts
        {
            public PublicKey Bag { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey Authority { get; set; }
        }

        public static class BagboxSessionProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Bag, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Attack(AttackAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Bag, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8603554577309964997UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}