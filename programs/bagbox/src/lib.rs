use anchor_lang::prelude::*;

declare_id!("DJyyu59JEsWPqZ5Q1v2DUgVAtHse3icGBGdbPMfftfAY");

/// Smart Contract for a simple game bagbox.
///
/// Each player has a bag, they can punch.
/// Each punch increases the damage of the bag.

#[program]
pub mod bagbox {
    use super::*;

    pub fn initialize(ctx: Context<Initialize>) -> Result<()> {
        let player = &mut ctx.accounts.player;
        player.authority = ctx.accounts.authority.key();

        let bag = &mut ctx.accounts.bag;
        bag.player = ctx.accounts.player.key();
        bag.damage = 0;
        Ok(())
    }

    pub fn punch(ctx: Context<Punch>) -> Result<()> {
        let bag = &mut ctx.accounts.bag;
        bag.damage = bag.damage.checked_add(1).unwrap();

        msg!("Bag damage: {}", bag.damage);

        Ok(())
    }
}

#[derive(Accounts)]
pub struct Initialize<'info> {
    #[account(
        init,
        payer = payer,
        space = 8 + Player::INIT_SPACE,
        seeds = [b"player".as_ref(), authority.key().as_ref()],
        bump,
    )]
    player: Account<'info, Player>,

    #[account(
        init,
        payer = payer,
        space = 8 + Bag::INIT_SPACE,
        seeds = [b"bag".as_ref(), authority.key().as_ref()],
        bump,
    )]
    bag: Account<'info, Bag>,

    #[account(mut)]
    payer: Signer<'info>,
    authority: Signer<'info>,
    system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct Punch<'info> {
    #[account(
        mut,
        seeds = [b"bag".as_ref(), authority.key().as_ref()],
        bump,
        has_one = player
    )]
    bag: Account<'info, Bag>,

    #[account(
        seeds = [b"player".as_ref(), authority.key().as_ref()],
        bump,
        has_one = authority
    )]
    player: Account<'info, Player>,
    authority: Signer<'info>,
}

#[account]
#[derive(InitSpace, Debug)]
pub struct Player {
    authority: Pubkey,
}

#[account]
#[derive(InitSpace, Debug)]
pub struct Bag {
    player: Pubkey,
    damage: u8,
}
