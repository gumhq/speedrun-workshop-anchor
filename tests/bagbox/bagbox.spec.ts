import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { Bagbox } from "../../target/types/bagbox";
import { expect } from "chai";

describe("Bagbox", () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());

  const program = anchor.workspace.Bagbox as Program<Bagbox>;
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

  it("should punch causing damage to the bag", async () => {
    const bagAccountPre = await program.account.bag.fetch(bag);
    expect(bagAccountPre.damage).to.eql(0);

    await program.methods
      .punch()
      .accounts({
        player: player,
        bag: bag,
      })
      .rpc();

    const bagAccountPost = await program.account.bag.fetch(bag);
    expect(bagAccountPost.damage).to.eql(bagAccountPre.damage + 1);
  });
});
