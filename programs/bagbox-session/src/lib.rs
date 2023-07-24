use anchor_lang::prelude::*;
use gpl_session::{session_auth_or, Session, SessionError, SessionToken};

declare_id!("5HjwvteZtoz5Ap887n9sDEykzHQg5pJA4iFV2ofJZ1yv");

/// Smart Contract for a simple game bagbox.
///
/// Each player has a bag, they can punch.
/// Each punch increases the damage of the bag.

#[program]
pub mod bagbox_session {
    use super::*;

    pub fn initialize(ctx: Context<Initialize>) -> Result<()> {
        let player = &mut ctx.accounts.player;
        player.authority = ctx.accounts.authority.key();

        let bag = &mut ctx.accounts.bag;
        bag.player = ctx.accounts.player.key();
        bag.damage = 0;
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.authority.key(),
        BagboxError::InvalidSessionToken
    )]
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

#[derive(Accounts, Session)]
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

    #[session(
        // Ephemeral Signer
        signer = signer,
        // The authority of the session token and the player account must match
        authority = authority.key()
    )]
    session_token: Option<Account<'info, SessionToken>>,

    #[account(mut)]
    signer: Signer<'info>,

    /// CHECK: This the actual authority of the player account
    authority: AccountInfo<'info>,
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

#[error_code]
pub enum BagboxError {
    #[msg("The session token is invalid")]
    InvalidSessionToken,
}
