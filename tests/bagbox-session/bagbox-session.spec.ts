import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { BagboxSession } from "../target/types/bagbox_session";
import { expect } from "chai";
import { SessionTokenManager } from "@gumhq/sdk";
import { Keypair, PublicKey } from "@solana/web3.js";

describe("Bagbox with Session", () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());

  const program = anchor.workspace.BagboxSession as Program<BagboxSession>;
  const provider = anchor.getProvider();

  let bag: anchor.web3.PublicKey;
  let player: anchor.web3.PublicKey;

  it("should intialize player and bag", async () => {
    const txKeys = await program.methods
      .initialize()
      .accounts({
        // @ts-ignore
        authority: program.provider.wallet.publicKey,
        // @ts-ignore
        payer: program.provider.wallet.publicKey,
      })
      .rpcAndKeys();

    bag = txKeys.pubkeys.bag;
    player = txKeys.pubkeys.player;

    const bagAccount = await program.account.bag.fetch(bag);
    expect(bagAccount.player).to.eql(player);
    expect(bagAccount.damage).to.eql(0);

    const playerAccount = await program.account.player.fetch(player);
    // @ts-ignore
    expect(playerAccount.authority).to.eql(provider.wallet.publicKey);
  });

  it("should attack causing damage to the bag", async () => {
    const bagAccountPre = await program.account.bag.fetch(bag);
    expect(bagAccountPre.damage).to.eql(0);

    await program.methods
      .attack()
      .accounts({
        player: player,
        bag: bag,
        sessionToken: null,
      })
      .rpc();

    const bagAccountPost = await program.account.bag.fetch(bag);
    expect(bagAccountPost.damage).to.eql(bagAccountPre.damage + 1);
  });

  describe("Play with Session Token", async () => {
    //FIXME: Do this with a new wallet, so that it is clean
    let sessionTokenManager = new SessionTokenManager(
      // @ts-ignore
      provider.wallet,
      provider.connection,
      "localnet"
    );
    const sessionSigner = Keypair.generate();
    let sessionToken: PublicKey;

    before(async () => {
      // Create a new session token for the player.
      const sessionTxKeys = await sessionTokenManager.program.methods
        .createSession(true, null)
        .accounts({
          // @ts-ignore
          authority: provider.wallet.publicKey,
          sessionSigner: sessionSigner.publicKey,
          targetProgram: program.programId,
        })
        .signers([sessionSigner])
        .rpcAndKeys();

      sessionToken = sessionTxKeys.pubkeys.sessionToken;
    });

    it("should attack causing damage to the bag", async () => {
      const bagAccountPre = await program.account.bag.fetch(bag);
      const txRpcKeys = await program.methods
        .attack()
        .accounts({
          // @ts-ignore
          authority: provider.wallet.publicKey,
          signer: sessionSigner.publicKey,
          sessionToken,
        })
        .signers([sessionSigner])
        .rpcAndKeys();

      const bagAccountPost = await program.account.bag.fetch(bag);
      expect(bagAccountPost.damage).to.eql(bagAccountPre.damage + 1);
    });

    // It should not allow the session token of a different player to be used.
  });
});
