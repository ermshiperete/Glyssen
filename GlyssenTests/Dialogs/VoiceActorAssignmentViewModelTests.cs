﻿using System;
using System.Collections.Generic;
using System.Linq;
using Glyssen;
using Glyssen.Character;
using Glyssen.Dialogs;
using GlyssenTests.Properties;
using NUnit.Framework;

namespace GlyssenTests.Dialogs
{
	[TestFixture]
	class VoiceActorAssignmentViewModelTests
	{
		private Project m_testProject;
		private VoiceActorAssignmentViewModel m_model;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			// Use a test version of the file so the tests won't break every time we fix a problem in the production control file.
			ControlCharacterVerseData.TabDelimitedCharacterVerseData = Resources.TestCharacterVerse;
			m_testProject = TestProject.CreateTestProject(TestProject.TestBook.MRK);
		}

		[SetUp]
		public void SetUp()
		{
			m_testProject.VoiceActorList.Actors.Clear();
			m_testProject.CharacterGroupList.CharacterGroups.Clear();
			// Adding one group here prevents the constructor from generating groups
			m_testProject.CharacterGroupList.CharacterGroups.Add(new CharacterGroup(1));
			m_model = new VoiceActorAssignmentViewModel(m_testProject);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			TestProject.DeleteTestProjectFolder();
		}

		[Test]
		public void AddNewGroup()
		{
			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);
			var newGroup = m_model.AddNewGroup();
			Assert.AreEqual(2, m_testProject.CharacterGroupList.CharacterGroups.Count);
			Assert.AreEqual(newGroup, m_testProject.CharacterGroupList.CharacterGroups[1]);
		}

		[Test]
		public void MoveActorFromGroupToGroup_SwapTrue()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var actor2 = new Glyssen.VoiceActor.VoiceActor { Id = 2 };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.AssignVoiceActor(actor1);
			var destGroup = m_model.AddNewGroup();
			destGroup.AssignVoiceActor(actor2);

			Assert.AreEqual(actor1, sourceGroup.VoiceActorAssigned);
			Assert.AreEqual(actor2, destGroup.VoiceActorAssigned);
			m_model.MoveActorFromGroupToGroup(sourceGroup, destGroup, true);
			Assert.AreEqual(actor2, sourceGroup.VoiceActorAssigned);
			Assert.AreEqual(actor1, destGroup.VoiceActorAssigned);
		}

		[Test]
		public void MoveActorFromGroupToGroup_SwapFalse()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var actor2 = new Glyssen.VoiceActor.VoiceActor { Id = 2 };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.AssignVoiceActor(actor1);
			var destGroup = m_model.AddNewGroup();
			destGroup.AssignVoiceActor(actor2);

			Assert.AreEqual(actor1, sourceGroup.VoiceActorAssigned);
			Assert.AreEqual(actor2, destGroup.VoiceActorAssigned);
			m_model.MoveActorFromGroupToGroup(sourceGroup, destGroup, false);
			Assert.False(sourceGroup.IsVoiceActorAssigned);
			Assert.AreEqual(actor1, destGroup.VoiceActorAssigned);
		}

		[Test]
		public void AssignActorToGroup_CanAssignTrue_ActorAssigned()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var group = m_model.CharacterGroups[0];
			m_model.AssignActorToGroup(actor1, group);
			Assert.AreEqual(actor1, group.VoiceActorAssigned);
		}

		[Test]
		public void AssignActorToGroup_CanAssignFalse_ActorNotAssigned()
		{
			m_model.CanAssign = false;
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var group = m_model.CharacterGroups[0];
			m_model.AssignActorToGroup(actor1, group);
			Assert.False(group.IsVoiceActorAssigned);
			m_model.CanAssign = true;
		}

		[Test]
		public void UnAssignActorFromGroup_ByGroup()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var group = m_model.CharacterGroups[0];
			m_model.AssignActorToGroup(actor1, group);
			Assert.True(group.IsVoiceActorAssigned);

			m_model.UnAssignActorFromGroup(group);
			Assert.False(group.IsVoiceActorAssigned);
		}

		[Test]
		public void UnAssignActorFromGroup_ByActor()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var group = m_model.CharacterGroups[0];
			m_model.AssignActorToGroup(actor1, group);
			Assert.True(group.IsVoiceActorAssigned);

			m_model.UnAssignActorFromGroup(actor1);
			Assert.False(group.IsVoiceActorAssigned);
		}

		[Test]
		public void MoveCharactersToGroup_DestinationContainsNoCharacters()
		{
			var characterIds = new List<string> { "John", "Andrew" };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.CharacterIds = new CharacterIdHashSet(characterIds);
			var destGroup = m_model.AddNewGroup();

			Assert.True(m_model.MoveCharactersToGroup(characterIds, destGroup));

			var destGroupCharacterIds = destGroup.CharacterIds;
			Assert.AreEqual(2, destGroup.CharacterIds.Count);
			Assert.True(destGroupCharacterIds.Contains("John"));
			Assert.True(destGroupCharacterIds.Contains("Andrew"));
		}

		[Test]
		public void MoveCharactersToGroup_DestinationContainsCharacters()
		{
			var characterIds = new List<string> { "John", "Andrew" };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.CharacterIds = new CharacterIdHashSet(characterIds);
			var destGroup = m_model.AddNewGroup();
			destGroup.CharacterIds = new CharacterIdHashSet { "ear", "mouth" };

			Assert.True(m_model.MoveCharactersToGroup(characterIds, destGroup));

			var destGroupCharacterIds = destGroup.CharacterIds;
			Assert.AreEqual(4, destGroup.CharacterIds.Count);
			Assert.True(destGroupCharacterIds.Contains("John"));
			Assert.True(destGroupCharacterIds.Contains("Andrew"));
		}

		[Test]
		public void MoveCharactersToGroup_SourceGroupSameAsDestinationGroup_NoActionTaken()
		{
			var characterIds = new List<string> { "John", "Andrew" };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.CharacterIds = new CharacterIdHashSet(characterIds);

			Assert.False(m_model.MoveCharactersToGroup(characterIds, sourceGroup));
		}

		[Test]
		public void MoveCharactersToGroup_AllCharactersMovedOutOfSource_SourceGroupRemoved()
		{
			var characterIds = new List<string> { "John", "Andrew" };
			var sourceGroup = m_model.CharacterGroups[0];
			sourceGroup.CharacterIds = new CharacterIdHashSet(characterIds);
			var destGroup = m_model.AddNewGroup();

			Assert.AreEqual(2, m_testProject.CharacterGroupList.CharacterGroups.Count);
			Assert.True(m_model.MoveCharactersToGroup(characterIds, destGroup));
			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);
		}

		[Test]
		public void MoveCharactersToGroup_NoCharacterProvided_ThrowsArgumentException()
		{
			var destGroup = m_model.CharacterGroups[0];
			Assert.Throws<ArgumentException>(() => m_model.MoveCharactersToGroup(new List<string>(), destGroup));
		}

		[Test]
		public void RemoveUnusedGroups_RemovesGroupWithNoCharactersAndNoAssignedActor()
		{
			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);
			m_model.RemoveUnusedGroups();
			Assert.AreEqual(0, m_testProject.CharacterGroupList.CharacterGroups.Count);
		}

		[Test]
		public void RemoveUnusedGroups_DoesNotRemoveGroupWithAssignedActor()
		{
			var groupWithActor = m_testProject.CharacterGroupList.CharacterGroups[0];
			groupWithActor.AssignVoiceActor(new Glyssen.VoiceActor.VoiceActor());
			m_model.AddNewGroup();
			Assert.AreEqual(2, m_testProject.CharacterGroupList.CharacterGroups.Count);
			m_model.RemoveUnusedGroups();
			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);
			Assert.AreEqual(groupWithActor, m_testProject.CharacterGroupList.CharacterGroups[0]);
			Assert.True(m_testProject.CharacterGroupList.CharacterGroups[0].IsVoiceActorAssigned);
		}

		[Test]
		public void RegenerateGroups()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var actor2 = new Glyssen.VoiceActor.VoiceActor { Id = 2 };
			var actor3 = new Glyssen.VoiceActor.VoiceActor { Id = 3 };
			m_testProject.VoiceActorList.Actors = new List<Glyssen.VoiceActor.VoiceActor> { actor1, actor2, actor3 };
			m_model.RegenerateGroups();
			Assert.AreEqual(3, m_testProject.CharacterGroupList.CharacterGroups.Count);
		}

		[Test]
		public void RegenerateGroups_HasCameoAssigned_MaintainsCameoGroup()
		{
			var actor1 = new Glyssen.VoiceActor.VoiceActor { Id = 1 };
			var actor2 = new Glyssen.VoiceActor.VoiceActor { Id = 2 };
			var actor3 = new Glyssen.VoiceActor.VoiceActor { Id = 3 };
			m_testProject.VoiceActorList.Actors = new List<Glyssen.VoiceActor.VoiceActor> { actor1, actor2, actor3 };
			m_model.RegenerateGroups();

			var actor4 = new Glyssen.VoiceActor.VoiceActor { Id = 4, IsCameo = true };
			m_testProject.VoiceActorList.Actors.Add(actor4);
			var cameoGroup = m_model.AddNewGroup();
			cameoGroup.AssignVoiceActor(actor4);
			m_model.MoveCharactersToGroup(new[] { "John" }, cameoGroup, false);

			m_model.RegenerateGroups();
			var groups = m_testProject.CharacterGroupList.CharacterGroups;
			Assert.AreEqual(4, groups.Count);

			cameoGroup = groups.First(g => g.VoiceActorAssigned == actor4);
			Assert.AreEqual(actor4, cameoGroup.VoiceActorAssigned);
			Assert.AreEqual(1, cameoGroup.CharacterIds.Count);
			Assert.True(cameoGroup.CharacterIds.Contains("John"));
			Assert.False(groups.Where(g => g != cameoGroup).SelectMany(g => g.CharacterIds).Contains("John"));
		}

		[Test]
		public void SplitGroup_MoveOne()
		{
			var characterIds = new List<string> { "John", "Andrew" };
			var existingGroup = m_model.CharacterGroups[0];
			existingGroup.CharacterIds = new CharacterIdHashSet(characterIds);

			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);

			Assert.True(m_model.SplitGroup(existingGroup, new List<string> { "John" }));

			Assert.AreEqual(2, m_testProject.CharacterGroupList.CharacterGroups.Count);

			var newGroup = m_testProject.CharacterGroupList.CharacterGroups[1];

			Assert.False(existingGroup == newGroup);
			Assert.True(existingGroup.CharacterIds.Contains("Andrew"));
			Assert.False(existingGroup.CharacterIds.Contains("John"));
			Assert.True(newGroup.CharacterIds.Contains("John"));
		}

		[Test]
		public void SplitGroup_MoveMultiple()
		{
			var characterIds = new List<string> { "John", "Andrew", "Peter" };
			var existingGroup = m_model.CharacterGroups[0];
			existingGroup.CharacterIds = new CharacterIdHashSet(characterIds);

			Assert.AreEqual(1, m_testProject.CharacterGroupList.CharacterGroups.Count);

			Assert.True(m_model.SplitGroup(existingGroup, new List<string> { "John", "Peter" }));

			Assert.AreEqual(2, m_testProject.CharacterGroupList.CharacterGroups.Count);

			var newGroup = m_testProject.CharacterGroupList.CharacterGroups[1];

			Assert.False(existingGroup == newGroup);
			Assert.True(existingGroup.CharacterIds.Contains("Andrew"));
			Assert.False(existingGroup.CharacterIds.Contains("John"));
			Assert.False(existingGroup.CharacterIds.Contains("Peter"));
			Assert.True(newGroup.CharacterIds.Contains("John"));
			Assert.True(newGroup.CharacterIds.Contains("Peter"));
		}
	}
}
