import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ProjectDetails } from './project-details';
import { Project, ProjectService } from '../../../../core/services/project';

describe('ProjectDetails', () => {
  let component: ProjectDetails;
  let fixture: ComponentFixture<ProjectDetails>;
  let projectServiceMock: {
    getProject: ReturnType<typeof vi.fn>;
    updateProject: ReturnType<typeof vi.fn>;
    deleteProject: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  const project: Project = {
    id: 'p1',
    name: 'Alpha',
    description: 'Alpha description',
    startDate: '2026-01-01T00:00:00',
    endDate: '2026-02-01T00:00:00',
    priority: 'High',
    status: 'In Progress',
    ownerId: 'o1',
    ownerName: 'Owner Name',
    tenantId: 't1',
    progress: 25,
    isActive: true,
    createdAt: '2026-01-01T00:00:00',
    taskCount: 4,
    completedTaskCount: 1,
  };

  beforeEach(async () => {
    projectServiceMock = {
      getProject: vi.fn().mockReturnValue(of(project)),
      updateProject: vi.fn().mockReturnValue(of(true)),
      deleteProject: vi.fn().mockReturnValue(of(true)),
    };
    routerMock = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ProjectDetails],
      providers: [
        { provide: ProjectService, useValue: projectServiceMock },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'p1' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectDetails);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load the selected project from the API', () => {
    expect(component).toBeTruthy();
    expect(projectServiceMock.getProject).toHaveBeenCalledWith('p1');
    expect(component.project?.name).toBe('Alpha');
    expect(component.loading).toBe(false);
    expect(component.loadError).toBe('');
  });

  it('should show an error state when loading fails', () => {
    projectServiceMock.getProject.mockReturnValue(
      throwError(() => ({ status: 404, error: { message: 'Project not found.' } }))
    );

    component.loadProject();

    expect(component.project).toBeNull();
    expect(component.loadError).toBe('Project not found.');
    expect(component.loading).toBe(false);
  });

  it('should require a project name when saving', () => {
    component.openEditModal();
    component.editForm.name = '   ';

    component.saveProject();

    expect(component.fieldErrors['name']).toBeTruthy();
    expect(projectServiceMock.updateProject).not.toHaveBeenCalled();
  });

  it('should reject an end date before the start date', () => {
    component.openEditModal();
    component.editForm.startDate = '2026-05-01';
    component.editForm.endDate = '2026-04-01';

    component.saveProject();

    expect(component.fieldErrors['endDate']).toBeTruthy();
    expect(projectServiceMock.updateProject).not.toHaveBeenCalled();
  });

  it('should save valid changes without sending a tenantId', () => {
    component.openEditModal();
    component.editForm.name = 'Renamed Project';

    component.saveProject();

    expect(projectServiceMock.updateProject).toHaveBeenCalledTimes(1);
    const [id, payload] = projectServiceMock.updateProject.mock.calls[0];
    expect(id).toBe('p1');
    expect(payload.name).toBe('Renamed Project');
    expect(payload.tenantId).toBeUndefined();
    expect(component.isEditModalOpen).toBe(false);
    expect(component.saveSuccess).toBe(true);
  });

  it('should surface API validation errors on save', () => {
    projectServiceMock.updateProject.mockReturnValue(
      throwError(() => ({ status: 400, error: { message: "Invalid project status 'Nope'." } }))
    );
    component.openEditModal();
    component.editForm.name = 'Renamed Project';

    component.saveProject();

    expect(component.saveError).toBe("Invalid project status 'Nope'.");
    expect(component.isEditModalOpen).toBe(true);
  });

  it('should delete the project and navigate back to the list', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteProject();

    expect(projectServiceMock.deleteProject).toHaveBeenCalledWith('p1');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/tenant/projects']);
  });
});
