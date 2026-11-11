# Animation working agreement for Arkmon

The user explicitly asked on 2026-11-08 to retain this workflow after repeated costly Elian animation failures. Apply it to future character animation work.

1. Inspect the real Unity character and existing animation assets first. Use one consistent character construction, shared part dimensions, one rigid prop and a common camera/ground plane.
2. Prepare actual connected shoulder/elbow/wrist and hip/knee/ankle/foot joints. Keep source part and bone lengths constant in ordinary motion. Use separate correctly oriented grip, relaxed and open supporting hands. Show a clear wrist/forearm connection and thumb direction.
3. Establish static poses in Unity before transitions: squat at pickup, carry, two-handed lift. Keep knees over planted feet, show weight support and keep the bucket below the face. Review silhouettes, body attachments, layering and both facing directions.
4. Show the user actual Unity captures of those poses and wait for their feedback before animating transitions. Clearly label generated artwork as design references; it is never evidence that Unity already implements the pose. Preserve accepted reference art.
5. After pose feedback, block a short action with held key poses. Review body mechanics in rough form before polishing. Develop one small action at a time. Use real movement references when working on timing or weight.
6. Add intentional breakdown poses and only then transitions. Inspect a complete normal-speed and slow-motion sequence, including the initial grip, stop, release, return and any loop/reset. Avoid fixed-time blending that causes feet or hands to slide.
7. Mechanical tests check fixed lengths, ownership, contact, object dimensions and task state. Passing them does NOT establish natural anatomy, attractive motion or visual approval. State precisely what was inspected and what remains subjective.
8. Do not cover an unsuitable pose with whole-limb resizing, whole-leg pendulum rotation, mismatched extra body layers, remote hand targets, duplicate gripping hands or hidden/floating support fists. Fix the construction or add appropriate artwork.
9. Keep review scenes separate from live courtyard tasks until the pose/action is accepted. Preserve existing user changes. No Windows build unless requested. Do not commit or bump the version unless a commit is requested.

Known failures to avoid: limb gaps after waking; rear boot too far forward; foot sliding/hovering; arms crossing or disappearing behind the torso; backwards wrists and duplicate handedness; arm growth at the well; bucket movement detached from grip; supporting hand drawn as a floating fist; attractive generated pose pictures treated as registered animation frames; technical tests used as a substitute for visual review.

## Character scale review

Before integrating new character artwork, open the courtyard scale review through **Arkmon > Animationsprobe > Größenprobe öffnen**. Compare the intended in-game figure against the current cast with a common ground line and the orthographic camera. Check that Unity preserves the source texture dimensions and that the review scales both image axes equally. Inspect visible soles, head height and shoulder width. For seated figures, use an adult head and shoulder reference and check the seat and footboard contacts. Check the actual gameplay camera after the neutral comparison because staging changes apparent size.

Project instructions: see AGENTS.md. Current rig/pose implementation and validation are recorded in README.md.
