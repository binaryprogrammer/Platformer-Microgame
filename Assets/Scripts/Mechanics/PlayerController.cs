using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        /// <summary>
        /// Initial Wall Jump Velocity at the start of the jump.
        /// </summary>
        public float wallJumpTakeOffSpeed = 7;

        /// <summary>
        /// The TOTAL number of wall jumps we're going to give the player before they have to touch FLAT ground again.
        /// </summary>
        public int maxWallJumps = 2;

        /// <summary>
        /// How long do we apply the wall jump force? The player will have no control of their player for this time.
        /// </summary>
        public float wallJumpForceDuration = 0.3f;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        int wallJumps;
        float wallJumpDir;
        bool jump;
        float wallJumpTimer;

        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            if (IsGrounded)
                wallJumps = maxWallJumps;

            if (controlEnabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    animator.SetTrigger("attack");
                    //Physics2D.ov
                }

                if (jumpState != JumpState.WallJumping)
                {
                    move.x = Input.GetAxis("Horizontal");
                    wallJumpDir = -move.x; //When we wall jump we want to move in the opposite direction we were going.
                }

                if (IsGrounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (IsWallJumpable && wallJumps > 0 && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }

            UpdateJumpState();
            base.Update();
        }

		void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump: //Prepare to jump happens once
                    if (IsGrounded)
                        jumpState = JumpState.Jumping;
                    else //we are wall jumping
                    {
                        --wallJumps;
                        wallJumpTimer = wallJumpForceDuration;
                        Schedule<PlayerJumped>(1).player = this; //1 because we want to wait until next frame to start the sfx
                        jumpState = JumpState.WallJumping;
                    }
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping: 
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.WallJumping: //Wall jumping happens over time
                    if (!IsGrounded)
                    {
                        wallJumpTimer -= Time.deltaTime;

                        if (wallJumpTimer <= 0) //We've "wallJumped" with enough force, transition to InFlight
                            jumpState = JumpState.InFlight; //we want to stay in the wall jump state for a short time.
					}
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (jumpState == JumpState.WallJumping)
			{
                move.x = wallJumpDir * model.jumpModifier;
                velocity.y = wallJumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            WallJumpable,
            PrepareToJump,
            Jumping,
            WallJumping,
            InFlight,
            Landed
        }
    }
}